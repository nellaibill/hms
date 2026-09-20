using HMS.Modules.ActivityLog.Application.Abstractions;
using HMS.Modules.ActivityLog.Contracts;
using HMS.Modules.ActivityLog.Domain;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.ActivityLog.Infrastructure.Repositories;

internal class ActivityLogRepository : IActivityLogRepository
{
    private readonly ActivityLogDbContext _dbContext;

    public ActivityLogRepository(ActivityLogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ActivityLogEntry entry, CancellationToken cancellationToken)
        => await _dbContext.Entries.AddAsync(entry, cancellationToken);

    public Task<ActivityLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Entries.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<PagedResult<ActivityLogEntry>> GetPagedAsync(ActivityLogListQuery query, CancellationToken cancellationToken)
    {
        var entries = _dbContext.Entries.AsNoTracking();

        if (query.From is { } from)
        {
            var fromUtc = AsUtc(from);
            entries = entries.Where(a => a.CreatedAt >= fromUtc);
        }

        if (query.To is { } to)
        {
            // A date-only "To" (midnight) means "through the end of that day".
            var toUtc = AsUtc(to);
            if (to.TimeOfDay == TimeSpan.Zero)
            {
                var exclusiveEnd = toUtc.AddDays(1);
                entries = entries.Where(a => a.CreatedAt < exclusiveEnd);
            }
            else
            {
                entries = entries.Where(a => a.CreatedAt <= toUtc);
            }
        }

        if (query.UserId is { } userId)
        {
            entries = entries.Where(a => a.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(query.Module))
        {
            var module = query.Module.Trim().ToLower();
            entries = entries.Where(a => a.Module.ToLower() == module);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var action = query.Action.Trim().ToLower();
            entries = entries.Where(a => a.Action.ToLower() == action);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            var entityType = query.EntityType.Trim().ToLower();
            entries = entries.Where(a => a.EntityType != null && a.EntityType.ToLower() == entityType);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            var entityId = query.EntityId.Trim();
            entries = entries.Where(a => a.EntityId == entityId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLike(query.Search.Trim())}%";
            entries = entries.Where(a =>
                (a.Description != null && EF.Functions.ILike(a.Description, pattern, "\\"))
                || (a.EntityId != null && EF.Functions.ILike(a.EntityId, pattern, "\\"))
                || (a.EntityType != null && EF.Functions.ILike(a.EntityType, pattern, "\\")));
        }

        var totalCount = await entries.CountAsync(cancellationToken);

        var items = await entries
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ActivityLogEntry>(items, query.Page, query.PageSize, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    // Npgsql rejects Kind=Unspecified for timestamptz; query-string dates arrive that way.
    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

    private static string EscapeLike(string value)
        => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
