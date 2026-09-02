using HMS.Modules.Masters.Application;
using HMS.Modules.Patients.Application.Abstractions;
using HMS.Modules.Patients.Application.Excel;
using HMS.Modules.Patients.Application.Mapping;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Patients.Application;

/// <summary>
/// The front door for bulk patient import — upload/status/history/report/commit-trigger. The
/// actual row-by-row parsing/validation and patient creation happen off the request thread, in
/// PatientImportValidationBackgroundService/PatientImportCommitBackgroundService; this service
/// only ever creates the batch record and enqueues work, or reads back what those background
/// passes have recorded.
/// </summary>
internal class PatientImportService : IPatientImportService
{
    // Generous enough for tens of thousands of rows of the template's columns, small enough
    // that an accidental non-Excel upload doesn't sit in the in-memory queue for long.
    private const long MaxFileSizeBytes = 25 * 1024 * 1024;

    private readonly IPatientImportRepository _repository;
    private readonly IPatientImportQueue _queue;
    private readonly IStateService _stateService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<PatientImportService> _logger;

    public PatientImportService(
        IPatientImportRepository repository,
        IPatientImportQueue queue,
        IStateService stateService,
        ITenantContext tenantContext,
        ILogger<PatientImportService> logger)
    {
        _repository = repository;
        _queue = queue;
        _stateService = stateService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<byte[]> GetTemplateAsync(CancellationToken cancellationToken)
    {
        var states = await _stateService.GetAllAsync(cancellationToken);
        var stateNames = states.Select(s => s.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        return PatientImportTemplateGenerator.Generate(stateNames);
    }

    public async Task<Result<ImportBatchResponse>> UploadAsync(string fileName, byte[] fileContent, Guid? uploadedBy, CancellationToken cancellationToken)
    {
        if (fileContent.Length == 0)
        {
            return Result<ImportBatchResponse>.Failure(PatientErrorCodes.ImportFileInvalid, "The uploaded file is empty.");
        }

        if (fileContent.Length > MaxFileSizeBytes)
        {
            return Result<ImportBatchResponse>.Failure(PatientErrorCodes.ImportFileTooLarge, $"The uploaded file exceeds the {MaxFileSizeBytes / 1024 / 1024} MB limit.");
        }

        if (!_tenantContext.IsResolved || _tenantContext.TenantId is null || _tenantContext.ConnectionString is null)
        {
            throw new InvalidOperationException("PatientImportService.UploadAsync was called without a tenant having been established for this request.");
        }

        var batch = PatientImportBatch.Create(fileName, uploadedBy);
        await _repository.AddBatchAsync(batch, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        await _queue.EnqueueValidationAsync(
            new PatientImportValidationQueueItem(batch.Id, fileContent, _tenantContext.TenantId.Value, _tenantContext.ConnectionString),
            cancellationToken);

        _logger.LogInformation("Patient import batch {BatchId} uploaded ({FileName}) and queued for validation", batch.Id, fileName);

        return Result<ImportBatchResponse>.Success(batch.ToResponse());
    }

    public async Task<Result<ImportBatchResponse>> GetBatchAsync(Guid batchId, CancellationToken cancellationToken)
    {
        var batch = await _repository.GetBatchByIdAsync(batchId, cancellationToken);
        return batch is null
            ? Result<ImportBatchResponse>.Failure(PatientErrorCodes.ImportBatchNotFound, $"Import batch '{batchId}' was not found.")
            : Result<ImportBatchResponse>.Success(batch.ToResponse());
    }

    public async Task<(IReadOnlyList<ImportBatchResponse> Items, int TotalCount)> GetBatchesPagedAsync(ImportBatchListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetBatchesPagedAsync(query.Page, query.PageSize, cancellationToken);
        return (items.Select(b => b.ToResponse()).ToList(), totalCount);
    }

    public async Task<Result<ImportRowPage>> GetRowsPagedAsync(Guid batchId, ImportRowListQuery query, CancellationToken cancellationToken)
    {
        var batch = await _repository.GetBatchByIdAsync(batchId, cancellationToken);
        if (batch is null)
        {
            return Result<ImportRowPage>.Failure(PatientErrorCodes.ImportBatchNotFound, $"Import batch '{batchId}' was not found.");
        }

        var (items, totalCount) = await _repository.GetRowsPagedAsync(batchId, query.Status, query.Page, query.PageSize, cancellationToken);
        return Result<ImportRowPage>.Success(new ImportRowPage(items.Select(r => r.ToResponse()).ToList(), totalCount));
    }

    public async Task<Result<byte[]>> GetReportAsync(Guid batchId, CancellationToken cancellationToken)
    {
        var batch = await _repository.GetBatchByIdAsync(batchId, cancellationToken);
        if (batch is null)
        {
            return Result<byte[]>.Failure(PatientErrorCodes.ImportBatchNotFound, $"Import batch '{batchId}' was not found.");
        }

        var invalidRows = await _repository.GetAllRowsByStatusAsync(batchId, ImportRowStatus.Invalid, cancellationToken);
        var commitFailedRows = await _repository.GetAllRowsByStatusAsync(batchId, ImportRowStatus.CommitFailed, cancellationToken);

        var reportRows = invalidRows.Concat(commitFailedRows)
            .OrderBy(r => r.RowNumber)
            .Select(r => new PatientImportReportRow(r.RowNumber, r.DeserializeRawData(), r.DeserializeErrors()))
            .ToList();

        return Result<byte[]>.Success(PatientImportReportGenerator.Generate(reportRows));
    }

    public async Task<Result<ImportBatchResponse>> CommitAsync(Guid batchId, Guid? committedBy, CancellationToken cancellationToken)
    {
        var batch = await _repository.GetBatchByIdAsync(batchId, cancellationToken);
        if (batch is null)
        {
            return Result<ImportBatchResponse>.Failure(PatientErrorCodes.ImportBatchNotFound, $"Import batch '{batchId}' was not found.");
        }

        if (batch.Status != ImportBatchStatus.ReadyForReview)
        {
            return Result<ImportBatchResponse>.Failure(
                PatientErrorCodes.ImportBatchNotReady,
                $"Import batch '{batchId}' is in status '{batch.Status}' and cannot be committed. It must be ReadyForReview.");
        }

        if (!_tenantContext.IsResolved || _tenantContext.TenantId is null || _tenantContext.ConnectionString is null)
        {
            throw new InvalidOperationException("PatientImportService.CommitAsync was called without a tenant having been established for this request.");
        }

        batch.StartCommit(committedBy);
        await _repository.SaveChangesAsync(cancellationToken);

        await _queue.EnqueueCommitAsync(
            new PatientImportCommitQueueItem(batch.Id, _tenantContext.TenantId.Value, _tenantContext.ConnectionString, committedBy),
            cancellationToken);

        _logger.LogInformation("Patient import batch {BatchId} confirmed by {CommittedBy} and queued for commit", batch.Id, committedBy);

        return Result<ImportBatchResponse>.Success(batch.ToResponse());
    }
}
