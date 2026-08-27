using HMS.Modules.Notifications.Domain;

namespace HMS.Modules.Notifications.Application.Abstractions;

internal interface INotificationPreferenceRepository
{
    Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken);

    Task<NotificationPreference?> GetByUserAndCategoryAsync(Guid userId, string category, CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationPreference>> GetByUserAsync(Guid userId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
