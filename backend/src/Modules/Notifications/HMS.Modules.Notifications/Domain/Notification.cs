using HMS.Modules.Notifications.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Notifications.Domain;

/// <summary>
/// The event itself — immutable once written (no Update method), regardless of how many
/// people receive it; see NotificationRecipient for the per-user fan-out. Written once per
/// call to INotificationService.NotifyAsync (added in a later phase), already rendered —
/// Title/Body are the resolved text, not the template's raw placeholders.
/// </summary>
internal class Notification : Entity
{
    public string TemplateKey { get; private set; } = null!;
    public string Category { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string Body { get; private set; } = null!;

    /// <summary>Which HMS module raised this (e.g. "Appointments") — informational, for
    /// admin/debugging screens, not used for authorization.</summary>
    public string SourceModule { get; private set; } = null!;

    /// <summary>Nullable pair identifying the record this notification is about (e.g.
    /// ("Appointment", the Appointment's Id)), so the frontend can deep-link back to it.
    /// Both null or both set — never one without the other.</summary>
    public string? SourceEntityType { get; private set; }
    public Guid? SourceEntityId { get; private set; }

    public NotificationSeverity Severity { get; private set; }

    // Required by EF Core materialization.
    private Notification()
    {
    }

    private Notification(
        Guid id,
        string templateKey,
        string category,
        string title,
        string body,
        string sourceModule,
        string? sourceEntityType,
        Guid? sourceEntityId,
        NotificationSeverity severity,
        Guid? createdBy)
        : base(id, createdBy)
    {
        TemplateKey = templateKey;
        Category = category;
        Title = title;
        Body = body;
        SourceModule = sourceModule;
        SourceEntityType = sourceEntityType;
        SourceEntityId = sourceEntityId;
        Severity = severity;
    }

    public static Notification Create(
        string templateKey,
        string category,
        string title,
        string body,
        string sourceModule,
        string? sourceEntityType,
        Guid? sourceEntityId,
        NotificationSeverity severity,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(templateKey, nameof(templateKey));
        Guard.AgainstNullOrWhiteSpace(category, nameof(category));
        Guard.AgainstNullOrWhiteSpace(title, nameof(title));
        Guard.AgainstNullOrWhiteSpace(body, nameof(body));
        Guard.AgainstNullOrWhiteSpace(sourceModule, nameof(sourceModule));

        if (sourceEntityType is null != sourceEntityId is null)
        {
            throw new ArgumentException("SourceEntityType and SourceEntityId must both be set or both be null.");
        }

        // Time-ordered UUID per docs/DatabaseArchitecture.md §4.
        return new Notification(
            Guid.CreateVersion7(),
            templateKey.Trim(),
            category.Trim().ToLowerInvariant(),
            title.Trim(),
            body.Trim(),
            sourceModule.Trim(),
            string.IsNullOrWhiteSpace(sourceEntityType) ? null : sourceEntityType.Trim(),
            sourceEntityId,
            severity,
            createdBy);
    }
}
