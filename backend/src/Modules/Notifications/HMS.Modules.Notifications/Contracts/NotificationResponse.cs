namespace HMS.Modules.Notifications.Contracts;

/// <summary>One recipient's view of a notification — the Id here is the
/// NotificationRecipient's Id (what "mark as read" targets), not the underlying
/// Notification's Id, which is carried separately as <see cref="NotificationId"/>.</summary>
public record NotificationResponse
{
    public Guid Id { get; init; }
    public Guid NotificationId { get; init; }
    public string TemplateKey { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string SourceModule { get; init; } = string.Empty;
    public string? SourceEntityType { get; init; }
    public Guid? SourceEntityId { get; init; }
    public NotificationSeverity Severity { get; init; }
    public bool IsRead { get; init; }
    public DateTime? ReadAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>Result of raising a notification — deliberately not a
/// <see cref="NotificationResponse"/>: a single Notify call fans out to N recipient rows,
/// so there's no one "recipient view" to return.</summary>
public record NotificationBroadcastResponse
{
    public Guid NotificationId { get; init; }
    public int RecipientCount { get; init; }
}

public record UnreadCountResponse
{
    public int Count { get; init; }
}
