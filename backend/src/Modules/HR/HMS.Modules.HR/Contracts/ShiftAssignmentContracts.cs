using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Contracts;

public record CreateShiftAssignmentRequest
{
    public Guid StaffId { get; init; }
    public Guid DepartmentId { get; init; }
    public Guid ShiftId { get; init; }
    public DateOnly RosterDate { get; init; }
    public AssignmentStatus Status { get; init; } = AssignmentStatus.Scheduled;
    public string? Remarks { get; init; }
}

// Every field is mutable via PUT — unlike CreateShiftAssignmentRequest, there is no
// natural-key field here to protect (ShiftAssignment has nothing analogous to Shift.Code).
public record UpdateShiftAssignmentRequest
{
    public Guid StaffId { get; init; }
    public Guid DepartmentId { get; init; }
    public Guid ShiftId { get; init; }
    public DateOnly RosterDate { get; init; }
    public AssignmentStatus Status { get; init; } = AssignmentStatus.Scheduled;
    public string? Remarks { get; init; }
}

public record ShiftAssignmentResponse
{
    public Guid Id { get; init; }
    public Guid StaffId { get; init; }
    public Guid DepartmentId { get; init; }
    public Guid ShiftId { get; init; }
    public DateOnly RosterDate { get; init; }
    public AssignmentStatus Status { get; init; }
    public string? Remarks { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class ShiftAssignmentListQuery : PagedRequest
{
}
