namespace HMS.Modules.Messaging.Contracts;

public record ConversationResponse
{
    public Guid Id { get; init; }
    public ConversationType Type { get; init; }
    public string? Title { get; init; }
    public DateTime? LastMessageAt { get; init; }

    /// <summary>Just ids — resolving display names/avatars is a frontend concern (a batched
    /// lookup against Identity's own Users API), not something this module reaches into
    /// Identity for. Keeps this module's one cross-module dependency (Notifications, for the
    /// new-message alert) from growing into two.</summary>
    public IReadOnlyList<Guid> ParticipantUserIds { get; init; } = [];

    public int UnreadCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>The caller is added as a participant automatically — <see cref="ParticipantUserIds"/>
/// lists only the *other* participants. A OneToOne request (exactly one other id) that
/// already has a conversation between the same two users returns that existing conversation
/// instead of creating a duplicate.</summary>
public record CreateConversationRequest
{
    public ConversationType Type { get; init; }

    /// <summary>Required for Group; ignored for OneToOne (see Conversation.Title's own doc
    /// comment).</summary>
    public string? Title { get; init; }

    public IReadOnlyList<Guid> ParticipantUserIds { get; init; } = [];
}
