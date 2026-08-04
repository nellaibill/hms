using HMS.Modules.HR.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Domain;

/// <summary>
/// Records which staff member is assigned to which shift on which date — the aggregate
/// root for Phase 2 of Shift Management. StaffId/DepartmentId are plain Guid references
/// with no backing entity yet (no Staff/Department module exists) and are validated only
/// for presence, at the Application/validator layer — same as ShiftId's presence check.
/// ShiftId's *existence* (does this Shift actually exist) is a separate, additional check
/// ShiftAssignmentService performs against IShiftRepository; this entity itself only
/// stores the id, per docs/DecisionLog.md's ID-reference convention for cross-aggregate
/// references (mirrors User.RoleId referencing Role by id, not by navigation).
/// </summary>
internal class ShiftAssignment : Entity
{
    public Guid StaffId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public Guid ShiftId { get; private set; }
    public DateOnly RosterDate { get; private set; }
    public AssignmentStatus Status { get; private set; } = AssignmentStatus.Scheduled;
    public string? Remarks { get; private set; }

    // Required by EF Core materialization.
    private ShiftAssignment()
    {
    }

    private ShiftAssignment(
        Guid id,
        Guid staffId,
        Guid departmentId,
        Guid shiftId,
        DateOnly rosterDate,
        AssignmentStatus status,
        string? remarks,
        Guid? createdBy)
        : base(id, createdBy)
    {
        StaffId = staffId;
        DepartmentId = departmentId;
        ShiftId = shiftId;
        RosterDate = rosterDate;
        Status = status;
        Remarks = remarks;
    }

    // Deliberately no guard clauses against Guid.Empty for StaffId/DepartmentId/ShiftId —
    // matches User.Create, which likewise never guards its Guid roleId parameter;
    // required-ness for Guid fields is enforced at the validator layer only, in both cases.
    public static ShiftAssignment Create(
        Guid staffId,
        Guid departmentId,
        Guid shiftId,
        DateOnly rosterDate,
        AssignmentStatus status,
        string? remarks,
        Guid? createdBy)
    {
        // Time-ordered UUID per docs/DatabaseArchitecture.md §4 — same convention every
        // other aggregate root in this codebase uses.
        return new ShiftAssignment(
            Guid.CreateVersion7(),
            staffId,
            departmentId,
            shiftId,
            rosterDate,
            status,
            remarks?.Trim(),
            createdBy);
    }

    public void Update(
        Guid staffId,
        Guid departmentId,
        Guid shiftId,
        DateOnly rosterDate,
        AssignmentStatus status,
        string? remarks,
        Guid? updatedBy)
    {
        StaffId = staffId;
        DepartmentId = departmentId;
        ShiftId = shiftId;
        RosterDate = rosterDate;
        Status = status;
        Remarks = remarks?.Trim();
        MarkUpdated(updatedBy);
    }
}
