using System.Runtime.CompilerServices;
using System.Threading.Channels;
using HMS.Modules.Notifications.Application.Abstractions;

namespace HMS.Modules.Notifications.Infrastructure;

/// <summary>
/// Bounded in-memory queue backing INotificationDeliveryQueue — mirrors
/// HMS.Modules.Documents.Infrastructure.DocumentScanQueue exactly (same bounded-500,
/// wait-not-drop backpressure reasoning). Registered as a singleton (one queue for the
/// process's lifetime); NotificationDeliveryBackgroundService is the sole reader.
/// </summary>
internal class NotificationDeliveryQueue : INotificationDeliveryQueue
{
    // Bounded so a burst of notifications can't grow this without limit; a writer waits
    // rather than dropping work when full (BoundedChannelFullMode.Wait is the Channel default).
    private readonly Channel<NotificationDeliveryQueueItem> _channel = Channel.CreateBounded<NotificationDeliveryQueueItem>(500);

    public ValueTask EnqueueAsync(NotificationDeliveryQueueItem item, CancellationToken cancellationToken)
        => _channel.Writer.WriteAsync(item, cancellationToken);

    public async IAsyncEnumerable<NotificationDeliveryQueueItem> DequeueAllAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return item;
        }
    }
}
