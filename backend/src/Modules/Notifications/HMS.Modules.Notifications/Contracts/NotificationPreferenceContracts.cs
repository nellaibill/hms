namespace HMS.Modules.Notifications.Contracts;

public record NotificationPreferenceResponse
{
    public Guid Id { get; init; }
    public string Category { get; init; } = string.Empty;
    public bool InAppEnabled { get; init; }
    public bool EmailEnabled { get; init; }
    public bool SmsEnabled { get; init; }
}

/// <summary>Upserts the caller's preference for one category — if no row exists yet for
/// (caller, Category), one is created with these values rather than requiring a separate
/// "initialize my preferences" step.</summary>
public record UpdateNotificationPreferenceRequest
{
    public string Category { get; init; } = string.Empty;
    public bool InAppEnabled { get; init; }
    public bool EmailEnabled { get; init; }
    public bool SmsEnabled { get; init; }
}
