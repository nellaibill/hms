using HMS.Shared.Kernel;

namespace HMS.Modules.Messaging.Domain;

/// <summary>
/// One row per message. Soft-deletable (an author retracting a message) via the standard
/// Entity columns — no separate edit-history table, and no Update method: a message's Body
/// is immutable once sent, matching Notification's "immutable event" shape.
/// </summary>
internal class Message : Entity
{
    public Guid ConversationId { get; private set; }
    public Guid SenderId { get; private set; }
    public string Body { get; private set; } = null!;

    // Required by EF Core materialization.
    private Message()
    {
    }

    private Message(Guid id, Guid conversationId, Guid senderId, string body, Guid? createdBy)
        : base(id, createdBy)
    {
        ConversationId = conversationId;
        SenderId = senderId;
        Body = body;
    }

    public static Message Create(Guid conversationId, Guid senderId, string body, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(body, nameof(body));

        // Time-ordered UUID per docs/DatabaseArchitecture.md §4 — also what makes
        // conversation history naturally insert-ordered without relying on CreatedAt alone.
        return new Message(Guid.CreateVersion7(), conversationId, senderId, body.Trim(), createdBy);
    }
}
