using HMS.Modules.Notifications.Application.Abstractions;
using HMS.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Notifications.Infrastructure.Repositories;

internal class NotificationRepository : INotificationRepository
{
    private readonly NotificationsDbContext _dbContext;

    public NotificationRepository(NotificationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken)
        => await _dbContext.Notifications.AddAsync(notification, cancellationToken);

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
