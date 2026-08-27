using HMS.Modules.Notifications.Contracts;
using HMS.Modules.Notifications.Domain;

namespace HMS.Modules.Notifications.Application.Abstractions;

internal interface INotificationDeliveryRepository
{
    Task AddAsync(NotificationDelivery delivery, CancellationToken cancellationToken);

    Task AddRangeAsync(IEnumerable<NotificationDelivery> deliveries, CancellationToken cancellationToken);

    Task<NotificationDelivery?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The background delivery worker's (added in a later phase) work queue — the
    /// oldest <paramref name="batchSize"/> deliveries still <see cref="DeliveryStatus.Pending"/>.</summary>
    Task<IReadOnlyList<NotificationDelivery>> GetPendingAsync(int batchSize, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
