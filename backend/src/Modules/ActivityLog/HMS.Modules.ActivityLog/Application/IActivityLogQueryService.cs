using HMS.Modules.ActivityLog.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.ActivityLog.Application;

/// <summary>Public for the same CS0051 reason as IActivityLogService (the controller's
/// public constructor takes it). Read-only by design.</summary>
public interface IActivityLogQueryService
{
    Task<Result<PagedResult<ActivityLogResponse>>> GetPagedAsync(ActivityLogListQuery query, CancellationToken cancellationToken);

    Task<Result<ActivityLogDetailResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
