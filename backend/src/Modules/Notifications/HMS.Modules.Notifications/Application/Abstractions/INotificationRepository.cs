using HMS.Modules.Notifications.Domain;

namespace HMS.Modules.Notifications.Application.Abstractions;

internal interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken);

    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
