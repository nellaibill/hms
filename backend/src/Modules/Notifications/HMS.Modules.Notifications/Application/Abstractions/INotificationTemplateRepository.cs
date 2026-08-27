using HMS.Modules.Notifications.Contracts;
using HMS.Modules.Notifications.Domain;

namespace HMS.Modules.Notifications.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule in docs/Architecture.md — Application never references EF Core types.
/// </summary>
internal interface INotificationTemplateRepository
{
    Task AddAsync(NotificationTemplate template, CancellationToken cancellationToken);

    Task<NotificationTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<NotificationTemplate?> GetByKeyAndChannelAsync(string templateKey, NotificationChannel channel, CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationTemplate>> GetAllAsync(bool? isActive, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
