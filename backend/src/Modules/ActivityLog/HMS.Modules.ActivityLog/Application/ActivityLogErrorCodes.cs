namespace HMS.Modules.ActivityLog.Application;

/// <summary>Stable, machine-readable error codes for expected ActivityLog failures, per
/// docs/ApiStandards.md §5.</summary>
internal static class ActivityLogErrorCodes
{
    public const string NotFound = "ACTIVITY_LOG.NOT_FOUND";
    public const string InvalidDateRange = "ACTIVITY_LOG.INVALID_DATE_RANGE";
}
