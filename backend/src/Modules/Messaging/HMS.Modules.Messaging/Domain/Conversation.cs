using HMS.Modules.Messaging.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Messaging.Domain;

/// <summary>
/// A thread. OneToOne and Group share this table — the only structural difference is how
/// many ConversationParticipant rows point at it (exactly 2 vs 3+); nothing here
/// special-cases the count. Membership and per-user read state live on
/// ConversationParticipant, not here.
/// </summary>
internal class Conversation : Entity
{
    public ConversationType Type { get; private set; }

    /// <summary>Group name — null for OneToOne, where the UI derives a label from the other
    /// participant instead.</summary>
    public string? Title { get; private set; }

    /// <summary>Denormalized so the conversation list can sort/query without joining
    /// Messages. Bumped by <see cref="TouchLastMessage"/> whenever a message is sent (added
    /// in a later phase, alongside Message.Create).</summary>
    public DateTime? LastMessageAt { get; private set; }

    // Required by EF Core materialization.
    private Conversation()
    {
    }

    private Conversation(Guid id, ConversationType type, string? title, Guid? createdBy)
        : base(id, createdBy)
    {
        Type = type;
        Title = title;
    }

    public static Conversation CreateOneToOne(Guid? createdBy)
        // Time-ordered UUID per docs/DatabaseArchitecture.md §4.
        => new(Guid.CreateVersion7(), ConversationType.OneToOne, title: null, createdBy);

    public static Conversation CreateGroup(string title, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(title, nameof(title));

        return new Conversation(Guid.CreateVersion7(), ConversationType.Group, title.Trim(), createdBy);
    }

    public void RenameGroup(string title, Guid? updatedBy)
    {
        if (Type != ConversationType.Group)
        {
            throw new InvalidOperationException("Only a Group conversation can be renamed.");
        }

        Guard.AgainstNullOrWhiteSpace(title, nameof(title));

        Title = title.Trim();
        MarkUpdated(updatedBy);
    }

    public void TouchLastMessage(DateTime sentAt) => LastMessageAt = sentAt;
}
