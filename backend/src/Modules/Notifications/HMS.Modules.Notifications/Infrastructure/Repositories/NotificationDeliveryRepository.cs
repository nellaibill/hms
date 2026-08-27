using HMS.Modules.Notifications.Application.Abstractions;
using HMS.Modules.Notifications.Contracts;
using HMS.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Notifications.Infrastructure.Repositories;

internal class NotificationDeliveryRepository : INotificationDeliveryRepository
{
    private readonly NotificationsDbContext _dbContext;

    public NotificationDeliveryRepository(NotificationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(NotificationDelivery delivery, CancellationToken cancellationToken)
        => await _dbContext.NotificationDeliveries.AddAsync(delivery, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<NotificationDelivery> deliveries, CancellationToken cancellationToken)
        => await _dbContext.NotificationDeliveries.AddRangeAsync(deliveries, cancellationToken);

    public Task<NotificationDelivery?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.NotificationDeliveries.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<NotificationDelivery>> GetPendingAsync(int batchSize, CancellationToken cancellationToken)
        => await _dbContext.NotificationDeliveries
            .Where(d => d.Status == DeliveryStatus.Pending)
            .OrderBy(d => d.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
