using System.Text.Json;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Application.Abstractions;
using HMS.Modules.Patients.Application.Mapping;
using HMS.Modules.Patients.Contracts;
using HMS.Shared.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Patients.Infrastructure;

/// <summary>
/// Drains IPatientImportQueue's commit queue — the pass triggered only by the Super Admin's
/// explicit confirmation (PatientImportService.CommitAsync). For each Valid row, calls
/// IPatientService.CreateAsync — the exact method manual registration uses, so UHID
/// assignment, the transaction, and the duplicate re-check all come for free. Each row is
/// processed in its own fresh DI scope (its own DbContext), not shared across the batch: if one
/// row's creation throws, that scope's DbContext is simply discarded rather than needing
/// recovery, and every other row's own scope is unaffected. Registered as a hosted service
/// (singleton) — mirrors PatientImportValidationBackgroundService/DocumentScanBackgroundService.
/// </summary>
internal class PatientImportCommitBackgroundService : BackgroundService
{
    private readonly IPatientImportQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PatientImportCommitBackgroundService> _logger;

    public PatientImportCommitBackgroundService(IPatientImportQueue queue, IServiceScopeFactory scopeFactory, ILogger<PatientImportCommitBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _queue.DequeueCommitAsync(stoppingToken))
        {
            try
            {
                await CommitBatchAsync(item, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Mirrors PatientImportValidationBackgroundService: one bad batch must not stop
                // every subsequent one from ever committing.
                _logger.LogError(ex, "Failed to commit patient import batch {BatchId}; it remains in status Committing.", item.BatchId);
            }
        }
    }

    private async Task CommitBatchAsync(PatientImportCommitQueueItem item, CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> rowIds;
        using (var scope = _scopeFactory.CreateScope())
        {
            var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            tenantContext.SetTenant(item.TenantId, item.ConnectionString);

            var repository = scope.ServiceProvider.GetRequiredService<IPatientImportRepository>();
            rowIds = await repository.GetValidRowIdsAsync(item.BatchId, cancellationToken);
        }

        var createdCount = 0;
        var commitFailedCount = 0;

        foreach (var rowId in rowIds)
        {
            using var scope = _scopeFactory.CreateScope();
            var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            tenantContext.SetTenant(item.TenantId, item.ConnectionString);

            var repository = scope.ServiceProvider.GetRequiredService<IPatientImportRepository>();
            var patientService = scope.ServiceProvider.GetRequiredService<IPatientService>();

            var row = await repository.GetRowByIdAsync(rowId, cancellationToken);
            var request = row?.DeserializeMappedRequest();
            if (row is null || request is null)
            {
                commitFailedCount++;
                continue;
            }

            var result = await patientService.CreateAsync(request, actorId: item.CommittedBy, cancellationToken);
            if (result.IsSuccess)
            {
                row.MarkCreated(result.Value!.Id);
                createdCount++;
            }
            else
            {
                row.MarkCommitFailed(JsonSerializer.Serialize(new[] { new ImportRowError { Field = string.Empty, Message = result.Error! } }));
                commitFailedCount++;
            }

            await repository.SaveChangesAsync(cancellationToken);
        }

        using (var scope = _scopeFactory.CreateScope())
        {
            var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            tenantContext.SetTenant(item.TenantId, item.ConnectionString);

            var repository = scope.ServiceProvider.GetRequiredService<IPatientImportRepository>();
            var batch = await repository.GetBatchByIdAsync(item.BatchId, cancellationToken);
            if (batch is null)
            {
                _logger.LogWarning("Patient import batch {BatchId} disappeared during commit.", item.BatchId);
                return;
            }

            batch.CompleteCommit(createdCount, commitFailedCount);
            await repository.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Patient import batch {BatchId} committed: {Created} patients created, {Failed} rows failed at commit.",
            item.BatchId, createdCount, commitFailedCount);
    }
}
