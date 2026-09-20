using HMS.Modules.ActivityLog.Contracts;

namespace HMS.Modules.ActivityLog.Application;

/// <summary>
/// Public (not internal): the module's write seam — every other module calls this
/// in-process, the same pattern as INotificationService. Never throws: an audit-write
/// failure is logged and swallowed so it can never change the outcome of the business
/// operation being audited.
/// </summary>
public interface IActivityLogService
{
    Task LogAsync(ActivityLogRequest request, CancellationToken cancellationToken = default);
}
