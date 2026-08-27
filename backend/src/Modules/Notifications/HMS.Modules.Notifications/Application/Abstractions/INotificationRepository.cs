using HMS.Modules.Notifications.Domain;

namespace HMS.Modules.Notifications.Application.Abstractions;

internal interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken);

    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Batched lookup for GetMyNotificationsAsync's page of NotificationRecipient
    /// rows — one query per page instead of one per row (mirrors HMS.Modules.Identity's
    /// UserService.GetPagedAsync/IRoleRepository.GetManyByIdsAsync).</summary>
    Task<IReadOnlyList<Notification>> GetManyByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
