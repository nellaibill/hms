namespace HMS.Modules.Documents.Application.Abstractions;

/// <summary>
/// One queued scan job — carries the tenant's connection info alongside the document id
/// because the background reader (Infrastructure.DocumentScanBackgroundService) has no HTTP
/// request of its own to resolve a tenant from (see ITenantContext's remarks: it's populated
/// per-request by TenantResolutionMiddleware, which never runs for a background service's own
/// DI scope). Without this, every dequeue would resolve DocumentsDbContext with an
/// unestablished tenant and throw, leaving every document stuck Pending forever regardless of
/// how quickly it's drained — not merely an in-memory-queue-loses-work-on-restart risk (see
/// this queue's own doc comment on that), but every single scan, always.
/// </summary>
internal readonly record struct ScanQueueItem(Guid DocumentId, Guid TenantId, string ConnectionString);

/// <summary>
/// A single-process, in-memory work queue of newly-uploaded documents awaiting a virus scan —
/// the platform's first background-job mechanism (see
/// docs/modules/Documents/DocumentManagement.md; nothing like Hangfire/Quartz exists
/// elsewhere in this codebase yet, and introducing one is out of proportion to this one
/// pipeline). Backed by a bounded <c>System.Threading.Channels.Channel&lt;ScanQueueItem&gt;</c>
/// in Infrastructure.DocumentScanQueue, drained by Infrastructure.DocumentScanBackgroundService.
///
/// Being in-memory means a queued item is lost if the process restarts before it's drained —
/// acceptable for an MVP virus-scan pipeline (the document stays
/// <see cref="Contracts.DocumentStatus.Pending"/> forever rather than being silently marked
/// Available), but not a substitute for a durable queue if this pipeline grows
/// higher-stakes responsibilities later.
/// </summary>
internal interface IDocumentScanQueue
{
    ValueTask EnqueueAsync(ScanQueueItem item, CancellationToken cancellationToken);

    IAsyncEnumerable<ScanQueueItem> DequeueAllAsync(CancellationToken cancellationToken);
}
