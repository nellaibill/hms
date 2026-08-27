using HMS.Shared.Kernel;

namespace HMS.Modules.Notifications.Domain;

/// <summary>
/// Fan-out — one row per (notification, user). This *is* the in-app channel: no separate
/// NotificationDelivery row is written for InApp, because this row existing already means
/// "delivered" (a database insert can't partially fail the way an outbound email/SMS call
/// can, so there's nothing to retry). Backs "my notifications", the unread-count badge, and
/// mark-as-read.
/// </summary>
internal class NotificationRecipient : Entity
{
    public Guid NotificationId { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }

    // Required by EF Core materialization.
    private NotificationRecipient()
    {
    }

    private NotificationRecipient(Guid id, Guid notificationId, Guid userId, Guid? createdBy)
        : base(id, createdBy)
    {
        NotificationId = notificationId;
        UserId = userId;
        IsRead = false;
    }

    public static NotificationRecipient Create(Guid notificationId, Guid userId, Guid? createdBy)
        // Time-ordered UUID per docs/DatabaseArchitecture.md §4.
        => new(Guid.CreateVersion7(), notificationId, userId, createdBy);

    /// <summary>Idempotent — marking an already-read notification read again is a no-op, not
    /// an error, so "mark all as read" can call this on every unread row without checking
    /// state first.</summary>
    public void MarkAsRead(DateTime readAt)
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAt = readAt;
    }
}
