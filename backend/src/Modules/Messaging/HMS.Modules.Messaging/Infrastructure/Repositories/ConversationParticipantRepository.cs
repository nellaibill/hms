using HMS.Modules.Messaging.Application.Abstractions;
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

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
