using HMS.Modules.Notifications.Application.Abstractions;
using HMS.Modules.Notifications.Domain;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Notifications.Infrastructure.Repositories;

internal class NotificationRecipientRepository : INotificationRecipientRepository
{
    private readonly NotificationsDbContext _dbContext;

    public NotificationRecipientRepository(NotificationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(NotificationRecipient recipient, CancellationToken cancellationToken)
        => await _dbContext.NotificationRecipients.AddAsync(recipient, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<NotificationRecipient> recipients, CancellationToken cancellationToken)
        => await _dbContext.NotificationRecipients.AddRangeAsync(recipients, cancellationToken);

    public Task<NotificationRecipient?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.NotificationRecipients.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<PagedResult<NotificationRecipient>> GetByUserAsync(Guid userId, bool? isRead, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _dbContext.NotificationRecipients.Where(r => r.UserId == userId);

        if (isRead.HasValue)
        {
            query = query.Where(r => r.IsRead == isRead.Value);
        }

        query = query.OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<NotificationRecipient>(items, page, pageSize, totalCount);
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken)
        => _dbContext.NotificationRecipients.CountAsync(r => r.UserId == userId && !r.IsRead, cancellationToken);

    public async Task<IReadOnlyList<NotificationRecipient>> GetUnreadByUserAsync(Guid userId, CancellationToken cancellationToken)
        => await _dbContext.NotificationRecipients
            .Where(r => r.UserId == userId && !r.IsRead)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
