using HMS.Shared.Kernel;

namespace HMS.Modules.Notifications.Domain;

/// <summary>
/// Per user, per category (e.g. "appointment", "billing", "diagnostics"), which channels
/// they've opted into — coarse-grained by category rather than per exact event key, since
/// nobody needs dozens of individual toggles. In-app is always delivered regardless of this
/// row (it's the low-cost default — see NotificationRecipient); this only governs Email/Sms.
/// A missing row for a (user, category) pair means "use the default" (in-app + email on,
/// sms off) rather than "no preference recorded" — callers that need the default should
/// treat a null lookup that way instead of writing a row for every user up front.
/// </summary>
internal class NotificationPreference : Entity
{
    public Guid UserId { get; private set; }
    public string Category { get; private set; } = null!;
    public bool InAppEnabled { get; private set; }
    public bool EmailEnabled { get; private set; }
    public bool SmsEnabled { get; private set; }

    // Required by EF Core materialization.
    private NotificationPreference()
    {
    }

    private NotificationPreference(
        Guid id,
        Guid userId,
        string category,
        bool inAppEnabled,
        bool emailEnabled,
        bool smsEnabled,
        Guid? createdBy)
        : base(id, createdBy)
    {
        UserId = userId;
        Category = category;
        InAppEnabled = inAppEnabled;
        EmailEnabled = emailEnabled;
        SmsEnabled = smsEnabled;
    }

    /// <summary>Defaults mirror the "missing row" default described in this type's own doc
    /// comment (in-app + email on, sms off), so explicitly creating a row and never creating
    /// one behave the same until the user actually changes something.</summary>
    public static NotificationPreference Create(Guid userId, string category, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(category, nameof(category));

        return new NotificationPreference(
            Guid.CreateVersion7(),
            userId,
            category.Trim().ToLowerInvariant(),
            inAppEnabled: true,
            emailEnabled: true,
            smsEnabled: false,
            createdBy);
    }

    public void UpdateChannels(bool inAppEnabled, bool emailEnabled, bool smsEnabled, Guid? updatedBy)
    {
        InAppEnabled = inAppEnabled;
        EmailEnabled = emailEnabled;
        SmsEnabled = smsEnabled;
        MarkUpdated(updatedBy);
    }
}
