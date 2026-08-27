using HMS.Modules.Messaging.Domain;

namespace HMS.Modules.Messaging.Application.Abstractions;

internal interface IConversationParticipantRepository
{
    Task AddAsync(ConversationParticipant participant, CancellationToken cancellationToken);

    Task AddRangeAsync(IEnumerable<ConversationParticipant> participants, CancellationToken cancellationToken);

    /// <summary>The authorization check every conversation-scoped action runs first — see
    /// ConversationParticipant's doc comment. Null means the caller isn't a member.</summary>
    Task<ConversationParticipant?> GetByConversationAndUserAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ConversationParticipant>> GetByConversationAsync(Guid conversationId, CancellationToken cancellationToken);

    /// <summary>"My conversations" — every conversation this user belongs to.</summary>
    Task<IReadOnlyList<ConversationParticipant>> GetByUserAsync(Guid userId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
