using HMS.Modules.Notifications.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Notifications.Application.Abstractions;

internal interface INotificationRecipientRepository
{
    Task AddAsync(NotificationRecipient recipient, CancellationToken cancellationToken);

    Task AddRangeAsync(IEnumerable<NotificationRecipient> recipients, CancellationToken cancellationToken);

    Task<NotificationRecipient?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Backs "my notifications" — <paramref name="isRead"/> null means no filter.</summary>
    Task<PagedResult<NotificationRecipient>> GetByUserAsync(Guid userId, bool? isRead, int page, int pageSize, CancellationToken cancellationToken);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Every unread row for this user — "mark all as read" loads exactly this set.</summary>
    Task<IReadOnlyList<NotificationRecipient>> GetUnreadByUserAsync(Guid userId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
