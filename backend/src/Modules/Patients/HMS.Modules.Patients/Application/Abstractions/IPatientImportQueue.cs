namespace HMS.Modules.Patients.Application.Abstractions;

/// <summary>Carries the tenant identity through to a background scope that has no HTTP request
/// of its own — see PatientImportValidationBackgroundService's remarks (mirrors Documents'
/// ScanQueueItem).</summary>
internal readonly record struct PatientImportValidationQueueItem(Guid BatchId, byte[] FileContent, Guid TenantId, string ConnectionString);

internal readonly record struct PatientImportCommitQueueItem(Guid BatchId, Guid TenantId, string ConnectionString, Guid? CommittedBy);

/// <summary>
/// A single-process, in-memory work queue for bulk patient import — the validate and commit
/// passes each get their own queue/background-service pair, following the same pattern as
/// Documents' IDocumentScanQueue (the platform's first background-job mechanism; nothing like
/// Hangfire/Quartz exists elsewhere in this codebase). Being in-memory means a queued item is
/// lost if the process restarts before it's drained — the batch is left stuck in Validating or
/// Committing rather than silently marked done; acceptable for the same reason it was
/// acceptable for the Documents scan pipeline.
/// </summary>
internal interface IPatientImportQueue
{
    ValueTask EnqueueValidationAsync(PatientImportValidationQueueItem item, CancellationToken cancellationToken);

    ValueTask EnqueueCommitAsync(PatientImportCommitQueueItem item, CancellationToken cancellationToken);

    IAsyncEnumerable<PatientImportValidationQueueItem> DequeueValidationAsync(CancellationToken cancellationToken);

    IAsyncEnumerable<PatientImportCommitQueueItem> DequeueCommitAsync(CancellationToken cancellationToken);
}
