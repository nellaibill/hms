namespace HMS.Modules.Notifications.Contracts;

/// <summary>
/// The one shape every notification is created from — whether the call comes from another
/// module's in-process <see cref="Application.INotificationService"/> call (a later phase
/// wires the real trigger points in Appointments/Patients/Billing/Pharmacy/IPD) or from the
/// admin "manual send" HTTP endpoint.
/// </summary>
public record NotifyRequest
{
    /// <summary>Looked up against the InApp-channel NotificationTemplate whenever
    /// <see cref="Body"/> is omitted — see that resolution in NotificationService.NotifyAsync.
    /// Always required (even when <see cref="Body"/> is supplied directly) so every
    /// notification is traceable to the event that raised it.</summary>
    public string TemplateKey { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    /// <summary>Always literal — short enough that templating it wasn't worth a second
    /// placeholder-substitution pass (see <see cref="Body"/> for the templated field).</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Literal body text. Omit (null/empty) to render the InApp template named by
    /// <see cref="TemplateKey"/> against <see cref="Placeholders"/> instead — the admin
    /// manual-send endpoint typically supplies this directly, while a template-driven
    /// trigger (a later phase's Appointments/Patients/etc. call sites) typically omits it.</summary>
    public string? Body { get; init; }

    /// <summary>Substituted into the template's <c>{{Key}}</c> tokens when <see cref="Body"/>
    /// is omitted — see Application.TemplateRenderer. Ignored when <see cref="Body"/> is
    /// supplied directly.</summary>
    public IReadOnlyDictionary<string, string>? Placeholders { get; init; }

    public string SourceModule { get; init; } = string.Empty;
    public string? SourceEntityType { get; init; }
    public Guid? SourceEntityId { get; init; }
    public NotificationSeverity Severity { get; init; } = NotificationSeverity.Normal;

    /// <summary>Explicit recipient list — resolving "everyone with role X" or reading
    /// NotificationPreferences to filter channels is a later phase; every recipient in this
    /// list gets the in-app notification unconditionally today.</summary>
    public IReadOnlyList<Guid> RecipientUserIds { get; init; } = [];
}
