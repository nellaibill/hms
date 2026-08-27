namespace HMS.Modules.Notifications.Contracts;

/// <summary>
/// The one shape every notification is created from — whether the call comes from another
/// module's in-process <see cref="Application.INotificationService"/> call (a later phase
/// wires the real trigger points in Appointments/Patients/Billing/Pharmacy/IPD) or from the
/// admin "manual send" HTTP endpoint. Title/Body are already-rendered text: template lookup
/// by <see cref="TemplateKey"/> is a later phase (Phase 1's <c>NotificationTemplate</c>
/// entity exists but isn't wired to rendering yet) — for now the caller supplies the final
/// text directly.
/// </summary>
public record NotifyRequest
{
    /// <summary>Informational/for future template lookup — not resolved against
    /// NotificationTemplate yet (see this type's own doc comment).</summary>
    public string TemplateKey { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string SourceModule { get; init; } = string.Empty;
    public string? SourceEntityType { get; init; }
    public Guid? SourceEntityId { get; init; }
    public NotificationSeverity Severity { get; init; } = NotificationSeverity.Normal;

    /// <summary>Explicit recipient list — resolving "everyone with role X" or reading
    /// NotificationPreferences to filter channels is a later phase; every recipient in this
    /// list gets the in-app notification unconditionally today.</summary>
    public IReadOnlyList<Guid> RecipientUserIds { get; init; } = [];
}
