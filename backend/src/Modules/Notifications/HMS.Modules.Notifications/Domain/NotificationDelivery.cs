using HMS.Modules.Notifications.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Notifications.Domain;

/// <summary>
/// Per-channel delivery attempt tracking for Email/Sms only — see NotificationRecipient's
/// doc comment for why InApp has no row here. Written by the background delivery worker
/// (added in a later phase), which is also the only writer of every state transition below.
/// </summary>
internal class NotificationDelivery : Entity
{
    public Guid NotificationRecipientId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
    public DateTime? SentAt { get; private set; }

    // Required by EF Core materialization.
    private NotificationDelivery()
    {
    }

    private NotificationDelivery(Guid id, Guid notificationRecipientId, NotificationChannel channel, Guid? createdBy)
        : base(id, createdBy)
    {
        NotificationRecipientId = notificationRecipientId;
        Channel = channel;
        Status = DeliveryStatus.Pending;
        Attempts = 0;
    }

    public static NotificationDelivery Create(Guid notificationRecipientId, NotificationChannel channel, Guid? createdBy)
    {
        if (channel == NotificationChannel.InApp)
        {
            throw new ArgumentException("InApp delivery has no NotificationDelivery row — see this type's doc comment.", nameof(channel));
        }

        // Time-ordered UUID per docs/DatabaseArchitecture.md §4.
        return new NotificationDelivery(Guid.CreateVersion7(), notificationRecipientId, channel, createdBy);
    }

    public void MarkSent(DateTime sentAt)
    {
        Attempts++;
        Status = DeliveryStatus.Sent;
        SentAt = sentAt;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        Guard.AgainstNullOrWhiteSpace(error, nameof(error));

        Attempts++;
        Status = DeliveryStatus.Failed;
        LastError = error.Trim();
    }

    /// <summary>The recipient's NotificationPreferences opted this channel out — recorded
    /// (rather than simply never creating the row) so an admin auditing "why didn't this
    /// person get an SMS" sees an explicit answer instead of an absence.</summary>
    public void MarkSkipped(string reason)
    {
        Guard.AgainstNullOrWhiteSpace(reason, nameof(reason));

        Status = DeliveryStatus.Skipped;
        LastError = reason.Trim();
    }
}
