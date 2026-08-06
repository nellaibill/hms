namespace HMS.Modules.Documents.Application.Abstractions;

/// <summary>
/// A single-process, in-memory work queue of newly-uploaded document ids awaiting a virus
/// scan — the platform's first background-job mechanism (see
/// docs/modules/Documents/DocumentManagement.md; nothing like Hangfire/Quartz exists
/// elsewhere in this codebase yet, and introducing one is out of proportion to this one
/// pipeline). Backed by a bounded <c>System.Threading.Channels.Channel&lt;Guid&gt;</c> in
/// Infrastructure.DocumentScanQueue, drained by Infrastructure.DocumentScanBackgroundService.
///
/// Being in-memory means a queued item is lost if the process restarts before it's drained —
/// acceptable for an MVP virus-scan pipeline (the document stays
/// <see cref="Contracts.DocumentStatus.Pending"/> forever rather than being silently marked
/// Available), but not a substitute for a durable queue if this pipeline grows
/// higher-stakes responsibilities later.
/// </summary>
internal interface IDocumentScanQueue
{
    ValueTask EnqueueAsync(Guid documentId, CancellationToken cancellationToken);

    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken);
}
