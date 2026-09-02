using System.Text.Json;
using FluentValidation;
using HMS.Modules.Masters.Application;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Application.Abstractions;
using HMS.Modules.Patients.Application.Excel;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Patients.Infrastructure;

/// <summary>
/// Drains IPatientImportQueue's validation queue. Parses the uploaded workbook, maps and
/// validates every row through the same CreatePatientRequestValidator manual registration
/// uses, and records a Valid/Invalid outcome per row — nothing is written to patients/addresses
/// here, only to patient_import_rows/patient_import_batches. Registered as a hosted service
/// (singleton), so it resolves scoped dependencies through a fresh DI scope per batch — mirrors
/// DocumentScanBackgroundService.
/// </summary>
internal class PatientImportValidationBackgroundService : BackgroundService
{
    // Bounds how many PatientImportRow entities the change tracker holds before a save+clear —
    // matters for very large files (tens of thousands of rows).
    private const int SaveChunkSize = 250;

    private readonly IPatientImportQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PatientImportValidationBackgroundService> _logger;

    public PatientImportValidationBackgroundService(IPatientImportQueue queue, IServiceScopeFactory scopeFactory, ILogger<PatientImportValidationBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _queue.DequeueValidationAsync(stoppingToken))
        {
            try
            {
                await ValidateBatchAsync(item, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A failure here must not crash the whole background service — it would
                // silently stop processing every subsequent upload for the rest of the
                // process's lifetime. The batch is simply left in Validating forever, visible
                // as stuck in Import History.
                _logger.LogError(ex, "Failed to validate patient import batch {BatchId}; it remains in status Validating.", item.BatchId);
            }
        }
    }

    private async Task ValidateBatchAsync(PatientImportValidationQueueItem item, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        // Must happen before anything resolves a tenant-aware DbContext — this scope has no
        // HTTP request of its own for TenantResolutionMiddleware to have populated
        // ITenantContext from (mirrors DocumentScanBackgroundService.ScanOneAsync).
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantContext.SetTenant(item.TenantId, item.ConnectionString);

        var repository = scope.ServiceProvider.GetRequiredService<IPatientImportRepository>();
        var patientRepository = scope.ServiceProvider.GetRequiredService<IPatientRepository>();
        var stateService = scope.ServiceProvider.GetRequiredService<IStateService>();
        var districtService = scope.ServiceProvider.GetRequiredService<IDistrictService>();
        var validator = scope.ServiceProvider.GetRequiredService<IValidator<CreatePatientRequest>>();

        var batch = await repository.GetBatchByIdAsync(item.BatchId, cancellationToken);
        if (batch is null)
        {
            _logger.LogWarning("Patient import batch {BatchId} was queued for validation but no longer exists.", item.BatchId);
            return;
        }

        IReadOnlyList<ParsedImportRow> parsedRows;
        try
        {
            using var stream = new MemoryStream(item.FileContent);
            parsedRows = PatientImportExcelParser.Parse(stream);
        }
        catch (PatientImportFileException ex)
        {
            batch.MarkValidationFailed();
            await repository.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("Patient import batch {BatchId} failed to parse: {Reason}", item.BatchId, ex.Message);
            return;
        }

        var referenceData = await PatientImportReferenceData.LoadAsync(stateService, districtService, cancellationToken);

        // Catches a duplicate appearing twice within the same file — FindDuplicateAsync below
        // only sees rows already committed to the database, not sibling rows in this same
        // upload.
        var seenWithinFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var validCount = 0;
        var invalidCount = 0;
        var pendingSaves = 0;

        foreach (var parsedRow in parsedRows)
        {
            var (request, errors) = await PatientImportRowMapper.MapAsync(parsedRow.Values, referenceData, cancellationToken);

            if (errors.Count == 0)
            {
                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                errors.AddRange(validationResult.Errors.Select(e => new ImportRowError { Field = e.PropertyName, Message = e.ErrorMessage }));
            }

            if (errors.Count == 0)
            {
                var dedupeKey = $"{request.PrimaryPhone}|{request.FirstName.Trim()}|{request.LastName.Trim()}".ToLowerInvariant();
                if (!seenWithinFile.Add(dedupeKey))
                {
                    errors.Add(new ImportRowError { Field = PatientImportColumns.PrimaryPhone, Message = "Duplicate of an earlier row in this same file (same name and phone number)." });
                }
                else
                {
                    var existing = await patientRepository.FindDuplicateAsync(request.PrimaryPhone, request.FirstName, request.LastName, request.IdProofNumber, cancellationToken);
                    if (existing is not null)
                    {
                        errors.Add(new ImportRowError { Field = PatientImportColumns.PrimaryPhone, Message = $"A patient named '{existing.FirstName} {existing.LastName}' with this phone number is already registered (UHID: {existing.Uhid})." });
                    }
                }
            }

            var rawDataJson = JsonSerializer.Serialize(parsedRow.Values);

            PatientImportRow row;
            if (errors.Count == 0)
            {
                row = PatientImportRow.CreateValid(batch.Id, parsedRow.RowNumber, rawDataJson, JsonSerializer.Serialize(request));
                validCount++;
            }
            else
            {
                row = PatientImportRow.CreateInvalid(batch.Id, parsedRow.RowNumber, rawDataJson, JsonSerializer.Serialize(errors));
                invalidCount++;
            }

            await repository.AddRowsAsync([row], cancellationToken);
            pendingSaves++;

            if (pendingSaves >= SaveChunkSize)
            {
                await repository.SaveChangesAsync(cancellationToken);
                repository.ClearTracking();
                pendingSaves = 0;

                // The batch entity was detached by ClearTracking — re-attach by re-fetching so
                // MarkReadyForReview below still has a tracked instance to save.
                batch = await repository.GetBatchByIdAsync(item.BatchId, cancellationToken) ?? batch;
            }
        }

        batch.MarkReadyForReview(parsedRows.Count, validCount, invalidCount);
        await repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Patient import batch {BatchId} validated: {Total} rows, {Valid} valid, {Invalid} invalid.",
            batch.Id, parsedRows.Count, validCount, invalidCount);
    }
}
