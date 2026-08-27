namespace HMS.Modules.HR.Contracts;

/// <summary>
/// GET /api/v1/hr/dashboard's response shape. PresentToday folds Late and HalfDay attendance
/// rows into the headline "present" count (a Late/HalfDay employee did still show up) — see
/// docs/DecisionLog.md ADR-036 for why, and AbsentToday/OnLeaveToday are each their own exact
/// Attendance.Status count for the current UTC calendar date.
/// </summary>
public record HrDashboardResponse
{
    public int TotalEmployees { get; init; }
    public int ActiveEmployees { get; init; }
    public int PresentToday { get; init; }
    public int AbsentToday { get; init; }
    public int OnLeaveToday { get; init; }
    public int PendingLeaveRequests { get; init; }
    public int ExpiringDocuments { get; init; }
}
