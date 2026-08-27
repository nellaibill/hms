using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Contracts;

public record CreateAttendanceRequest
{
    public Guid EmployeeId { get; init; }
    public DateOnly? AttendanceDate { get; init; }
    public DateTime? CheckInTime { get; init; }
    public DateTime? CheckOutTime { get; init; }
    public AttendanceStatus Status { get; init; }
    public string? Remarks { get; init; }
}

// EmployeeId/AttendanceDate are intentionally absent — the natural key, protected from change
// after creation (a correction changes the day's outcome, not which employee/day it belongs to).
public record UpdateAttendanceRequest
{
    public DateTime? CheckInTime { get; init; }
    public DateTime? CheckOutTime { get; init; }
    public AttendanceStatus Status { get; init; }
    public string? Remarks { get; init; }
}

public record CheckInRequest
{
    public Guid EmployeeId { get; init; }

    /// <summary>Defaults to the server's current UTC time when omitted.</summary>
    public DateTime? CheckInTime { get; init; }
}

public record CheckOutRequest
{
    public Guid EmployeeId { get; init; }

    /// <summary>Defaults to the server's current UTC time when omitted.</summary>
    public DateTime? CheckOutTime { get; init; }
}

/// <summary>EmployeeName/EmployeeCode are a same-schema join enrichment (Attendance and
/// Employee both live in "hr" — see Attendance's own remarks) included on every row, unlike
/// EmployeeResponse's cross-module Department/Designation name enrichment which is reserved
/// for the single-record read.</summary>
public record AttendanceResponse
{
    public Guid Id { get; init; }
    public Guid EmployeeId { get; init; }
    public string EmployeeCode { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public DateOnly AttendanceDate { get; init; }
    public DateTime? CheckInTime { get; init; }
    public DateTime? CheckOutTime { get; init; }
    public AttendanceStatus Status { get; init; }
    public string? Remarks { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class AttendanceListQuery : PagedRequest
{
    public Guid? EmployeeId { get; set; }
    public Guid? DepartmentId { get; set; }
    public AttendanceStatus? Status { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}
