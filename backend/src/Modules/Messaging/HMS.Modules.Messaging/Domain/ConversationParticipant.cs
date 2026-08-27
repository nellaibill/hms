using HMS.Shared.Kernel;

namespace HMS.Modules.Messaging.Domain;

/// <summary>
/// Membership *and* the authorization boundary — a user may read or send into a
/// conversation only if a row exists here (checked in the Application layer, added in a
/// later phase; mirrors HMS.Modules.Documents' IDocumentAccessPolicy). Also carries
/// per-user read state: a single <see cref="LastReadAt"/> timestamp rather than a
/// per-message read-receipt table — unread count for this participant is "messages in the
/// conversation, sent by someone else, with CreatedAt after LastReadAt". Deliberately the
/// cheapest structure that answers the actual requirement; see the design doc's §09 for why
/// a full read-receipt table was cut.
/// </summary>
internal class ConversationParticipant : Entity
{
    public Guid ConversationId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public DateTime? LastReadAt { get; private set; }

    // Required by EF Core materialization.
    private ConversationParticipant()
    {
    }

    private ConversationParticipant(Guid id, Guid conversationId, Guid userId, DateTime joinedAt, Guid? createdBy)
        : base(id, createdBy)
    {
        ConversationId = conversationId;
        UserId = userId;
        JoinedAt = joinedAt;
    }

    public static ConversationParticipant Create(Guid conversationId, Guid userId, DateTime joinedAt, Guid? createdBy)
        // Time-ordered UUID per docs/DatabaseArchitecture.md §4.
        => new(Guid.CreateVersion7(), conversationId, userId, joinedAt, createdBy);

    /// <summary>Called when this participant opens the conversation. Never moves
    /// <see cref="LastReadAt"/> backwards — callers always pass "now", but guarding here
    /// keeps the invariant true even against an out-of-order call.</summary>
    public void MarkRead(DateTime at)
    {
        if (LastReadAt.HasValue && at <= LastReadAt.Value)
        {
            return;
        }

        LastReadAt = at;
    }
}
