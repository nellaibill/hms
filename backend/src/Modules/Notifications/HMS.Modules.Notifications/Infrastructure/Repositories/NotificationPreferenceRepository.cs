using HMS.Modules.Notifications.Application.Abstractions;
using HMS.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Notifications.Infrastructure.Repositories;

internal class NotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly NotificationsDbContext _dbContext;

    public NotificationPreferenceRepository(NotificationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken)
        => await _dbContext.NotificationPreferences.AddAsync(preference, cancellationToken);

    public Task<NotificationPreference?> GetByUserAndCategoryAsync(Guid userId, string category, CancellationToken cancellationToken)
    {
        var normalized = category.Trim().ToLowerInvariant();
        return _dbContext.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId && p.Category == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationPreference>> GetByUserAsync(Guid userId, CancellationToken cancellationToken)
        => await _dbContext.NotificationPreferences.Where(p => p.UserId == userId).ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
