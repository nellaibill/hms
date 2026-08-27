using HMS.Modules.Messaging.Application.Abstractions;
using HMS.Modules.Messaging.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Messaging.Infrastructure.Repositories;

internal class ConversationRepository : IConversationRepository
{
    private readonly MessagingDbContext _dbContext;

    public ConversationRepository(MessagingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Conversation conversation, CancellationToken cancellationToken)
        => await _dbContext.Conversations.AddAsync(conversation, cancellationToken);

    public Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Conversations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
