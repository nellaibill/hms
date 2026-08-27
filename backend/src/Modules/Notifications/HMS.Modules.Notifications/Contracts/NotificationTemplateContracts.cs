namespace HMS.Modules.Notifications.Contracts;

public record NotificationTemplateResponse
{
    public Guid Id { get; init; }
    public string TemplateKey { get; init; } = string.Empty;
    public NotificationChannel Channel { get; init; }
    public string? Subject { get; init; }
    public string BodyTemplate { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateNotificationTemplateRequest
{
    public string TemplateKey { get; init; } = string.Empty;
    public NotificationChannel Channel { get; init; }
    public string? Subject { get; init; }
    public string BodyTemplate { get; init; } = string.Empty;
}

/// <summary>Content and active-state are edited together in one action — a separate
/// activate/deactivate endpoint would be one more route for a toggle this form already
/// carries.</summary>
public record UpdateNotificationTemplateRequest
{
    public string? Subject { get; init; }
    public string BodyTemplate { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
