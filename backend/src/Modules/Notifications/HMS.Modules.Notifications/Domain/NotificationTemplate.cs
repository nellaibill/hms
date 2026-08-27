using HMS.Modules.Notifications.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Notifications.Domain;

/// <summary>
/// What gets rendered and sent for one (event, channel) pair — e.g.
/// ("appointment.booked", Email). One row per channel a given event actually uses, not one
/// row per event, since the subject/body genuinely differ per channel (an SMS body has no
/// subject and needs to be short; an email body doesn't). Editable by hospital admins
/// (engagement.edit) without a redeploy.
/// </summary>
internal class NotificationTemplate : Entity
{
    public string TemplateKey { get; private set; } = null!;
    public NotificationChannel Channel { get; private set; }

    /// <summary>Email-only. Null for InApp/Sms, which have no subject line.</summary>
    public string? Subject { get; private set; }

    /// <summary>Placeholders like <c>{{PatientName}}</c>, substituted by the template
    /// renderer (added in a later phase) against the data the triggering module supplied.</summary>
    public string BodyTemplate { get; private set; } = null!;

    public bool IsActive { get; private set; }

    // Required by EF Core materialization.
    private NotificationTemplate()
    {
    }

    private NotificationTemplate(
        Guid id,
        string templateKey,
        NotificationChannel channel,
        string? subject,
        string bodyTemplate,
        Guid? createdBy)
        : base(id, createdBy)
    {
        TemplateKey = templateKey;
        Channel = channel;
        Subject = subject;
        BodyTemplate = bodyTemplate;
        IsActive = true;
    }

    public static NotificationTemplate Create(
        string templateKey,
        NotificationChannel channel,
        string? subject,
        string bodyTemplate,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(templateKey, nameof(templateKey));
        Guard.AgainstNullOrWhiteSpace(bodyTemplate, nameof(bodyTemplate));

        if (channel == NotificationChannel.Email && string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("An Email template requires a subject.", nameof(subject));
        }

        // Time-ordered UUID per docs/DatabaseArchitecture.md §4.
        return new NotificationTemplate(
            Guid.CreateVersion7(),
            templateKey.Trim(),
            channel,
            string.IsNullOrWhiteSpace(subject) ? null : subject.Trim(),
            bodyTemplate.Trim(),
            createdBy);
    }

    public void UpdateContent(string? subject, string bodyTemplate, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(bodyTemplate, nameof(bodyTemplate));

        if (Channel == NotificationChannel.Email && string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("An Email template requires a subject.", nameof(subject));
        }

        Subject = string.IsNullOrWhiteSpace(subject) ? null : subject.Trim();
        BodyTemplate = bodyTemplate.Trim();
        MarkUpdated(updatedBy);
    }

    public void Activate(Guid? updatedBy)
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        MarkUpdated(updatedBy);
    }

    public void Deactivate(Guid? updatedBy)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        MarkUpdated(updatedBy);
    }
}
