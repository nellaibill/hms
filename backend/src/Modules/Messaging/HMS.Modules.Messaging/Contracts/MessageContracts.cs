namespace HMS.Modules.Messaging.Contracts;

public record MessageResponse
{
    public Guid Id { get; init; }
    public Guid ConversationId { get; init; }
    public Guid SenderId { get; init; }
    public string Body { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record SendMessageRequest
{
    public string Body { get; init; } = string.Empty;
}
