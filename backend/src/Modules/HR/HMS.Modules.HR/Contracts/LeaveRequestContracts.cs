using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Contracts;

/// <summary>TotalDays is intentionally absent — always computed server-side from StartDate/
/// EndDate (see LeaveRequest.Create), never trusted from the caller.</summary>
public record CreateLeaveRequestRequest
{
    public Guid EmployeeId { get; init; }
    public Guid LeaveTypeId { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public record ApproveLeaveRequestRequest
{
    public string? Notes { get; init; }
}

public record RejectLeaveRequestRequest
{
    public string Reason { get; init; } = string.Empty;
}

/// <summary>EmployeeName/LeaveTypeName are a same-schema join enrichment (LeaveRequest,
/// Employee, and LeaveType all live in "hr") included on every row, mirroring
/// AttendanceResponse's EmployeeName/EmployeeCode.</summary>
public record LeaveRequestResponse
{
    public Guid Id { get; init; }
    public Guid EmployeeId { get; init; }
    public string EmployeeCode { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public Guid LeaveTypeId { get; init; }
    public string LeaveTypeName { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public int TotalDays { get; init; }
    public string Reason { get; init; } = string.Empty;
    public LeaveRequestStatus Status { get; init; }
    public Guid? ApprovedByUserId { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public string? DecisionNotes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class LeaveRequestListQuery : PagedRequest
{
    public Guid? EmployeeId { get; set; }
    public Guid? LeaveTypeId { get; set; }
    public LeaveRequestStatus? Status { get; set; }

    /// <summary>Filters on StartDate falling within [DateFrom, DateTo].</summary>
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}
