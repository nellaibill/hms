namespace HMS.Modules.Notifications.Application.Abstractions;

/// <summary>
/// One queued delivery job — carries the tenant's connection info alongside the delivery id
/// because the background reader (Infrastructure.NotificationDeliveryBackgroundService) has
/// no HTTP request of its own to resolve a tenant from. Mirrors
/// HMS.Modules.Documents.Application.Abstractions.ScanQueueItem exactly, including the same
/// reasoning: without this, every dequeue would resolve NotificationsDbContext with an
/// unestablished tenant and throw, leaving every Email/Sms delivery stuck Pending forever.
/// </summary>
internal readonly record struct NotificationDeliveryQueueItem(Guid NotificationDeliveryId, Guid TenantId, string ConnectionString);

/// <summary>
/// A single-process, in-memory work queue of Email/Sms deliveries awaiting send — reuses the
/// exact mechanism HMS.Modules.Documents built for its virus-scan pipeline
/// (Infrastructure.NotificationDeliveryQueue, a bounded
/// <c>System.Threading.Channels.Channel&lt;NotificationDeliveryQueueItem&gt;</c>) rather than
/// introducing a second background-job mechanism, per docs/DecisionLog.md ADR-029.
///
/// Being in-memory means a queued item is lost if the process restarts before it's drained —
/// the delivery row simply stays <see cref="Contracts.DeliveryStatus.Pending"/> forever
/// rather than being silently marked Sent; acceptable at MVP scale (in-app delivery, the
/// guaranteed channel, already succeeded by the time anything is queued here — see
/// NotificationService.NotifyAsync).
/// </summary>
internal interface INotificationDeliveryQueue
{
    ValueTask EnqueueAsync(NotificationDeliveryQueueItem item, CancellationToken cancellationToken);

    IAsyncEnumerable<NotificationDeliveryQueueItem> DequeueAllAsync(CancellationToken cancellationToken);
}
