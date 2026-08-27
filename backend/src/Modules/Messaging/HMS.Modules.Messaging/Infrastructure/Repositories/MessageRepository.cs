using HMS.Modules.Messaging.Application.Abstractions;
using HMS.Modules.Messaging.Domain;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Messaging.Infrastructure.Repositories;

internal class MessageRepository : IMessageRepository
{
    private readonly MessagingDbContext _dbContext;

    public MessageRepository(MessagingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Message message, CancellationToken cancellationToken)
        => await _dbContext.Messages.AddAsync(message, cancellationToken);

    public async Task<PagedResult<Message>> GetByConversationAsync(Guid conversationId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _dbContext.Messages.Where(m => m.ConversationId == conversationId);

        var totalCount = await query.CountAsync(cancellationToken);

        // Newest page first (Skip/Take against a descending order), then reversed so each
        // page itself reads oldest-to-newest — the natural order for chat scrollback.
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        return new PagedResult<Message>(items, page, pageSize, totalCount);
    }

    public Task<int> GetUnreadCountAsync(Guid conversationId, Guid excludingSenderId, DateTime? after, CancellationToken cancellationToken)
        => _dbContext.Messages.CountAsync(
            m => m.ConversationId == conversationId
                && m.SenderId != excludingSenderId
                && (after == null || m.CreatedAt > after),
            cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
