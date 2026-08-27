using HMS.Modules.Notifications.Application.Abstractions;
using HMS.Modules.Notifications.Contracts;
using HMS.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Notifications.Infrastructure.Repositories;

internal class NotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly NotificationsDbContext _dbContext;

    public NotificationTemplateRepository(NotificationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(NotificationTemplate template, CancellationToken cancellationToken)
        => await _dbContext.NotificationTemplates.AddAsync(template, cancellationToken);

    public Task<NotificationTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.NotificationTemplates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<NotificationTemplate?> GetByKeyAndChannelAsync(string templateKey, NotificationChannel channel, CancellationToken cancellationToken)
        => _dbContext.NotificationTemplates.FirstOrDefaultAsync(t => t.TemplateKey == templateKey && t.Channel == channel, cancellationToken);

    public async Task<IReadOnlyList<NotificationTemplate>> GetAllAsync(bool? isActive, CancellationToken cancellationToken)
    {
        var query = _dbContext.NotificationTemplates.AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(t => t.IsActive == isActive.Value);
        }

        return await query.OrderBy(t => t.TemplateKey).ThenBy(t => t.Channel).ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
