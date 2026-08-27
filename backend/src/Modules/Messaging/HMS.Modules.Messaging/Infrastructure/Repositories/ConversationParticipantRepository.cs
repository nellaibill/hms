using HMS.Modules.Messaging.Application.Abstractions;
using HMS.Modules.Messaging.Contracts;
using HMS.Modules.Messaging.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Messaging.Infrastructure.Repositories;

internal class ConversationParticipantRepository : IConversationParticipantRepository
{
    private readonly MessagingDbContext _dbContext;

    public ConversationParticipantRepository(MessagingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ConversationParticipant participant, CancellationToken cancellationToken)
        => await _dbContext.ConversationParticipants.AddAsync(participant, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<ConversationParticipant> participants, CancellationToken cancellationToken)
        => await _dbContext.ConversationParticipants.AddRangeAsync(participants, cancellationToken);

    public Task<ConversationParticipant?> GetByConversationAndUserAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken)
        => _dbContext.ConversationParticipants.FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<ConversationParticipant>> GetByConversationAsync(Guid conversationId, CancellationToken cancellationToken)
        => await _dbContext.ConversationParticipants.Where(p => p.ConversationId == conversationId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ConversationParticipant>> GetByUserAsync(Guid userId, CancellationToken cancellationToken)
        => await _dbContext.ConversationParticipants.Where(p => p.UserId == userId).ToListAsync(cancellationToken);

    public async Task<Guid?> FindOneToOneConversationIdAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken)
    {
        var userOneConversationIds = _dbContext.ConversationParticipants
            .Where(p => p.UserId == userId1)
            .Select(p => p.ConversationId);

        // Relies on OneToOne conversations always having exactly the 2 participants they
        // were created with (Conversation.Type is immutable, and ConversationService is the
        // only writer) — a OneToOne row containing both users therefore can't be a group
        // that happens to include them too.
        return await (
            from participant in _dbContext.ConversationParticipants
            join conversation in _dbContext.Conversations on participant.ConversationId equals conversation.Id
            where participant.UserId == userId2
                && conversation.Type == ConversationType.OneToOne
                && userOneConversationIds.Contains(participant.ConversationId)
            select (Guid?)participant.ConversationId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
