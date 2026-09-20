using HMS.Modules.ActivityLog.Contracts;
using HMS.Modules.ActivityLog.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.ActivityLog.Application.Abstractions;

/// <summary>Append + read only — there is intentionally no update or delete.</summary>
internal interface IActivityLogRepository
{
    Task AddAsync(ActivityLogEntry entry, CancellationToken cancellationToken);

    Task<ActivityLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ActivityLogEntry>> GetPagedAsync(ActivityLogListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
