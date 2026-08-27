using HMS.Modules.HR.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Domain;

/// <summary>
/// One employee's attendance outcome for one calendar day — the aggregate root backing both
/// the check-in/check-out flow and manual corrections (marking Absent/OnLeave without ever
/// checking in). EmployeeId is a real, same-schema FK (see AttendanceConfiguration) — unlike
/// Employee.DepartmentId/DesignationId, Attendance and Employee both live in the "hr" schema,
/// so a real database FK is used per docs/DecisionLog.md ADR-036. Enforces no domain
/// invariants around the check-in/check-out state machine itself (AlreadyCheckedIn/
/// NotCheckedIn/AlreadyCheckedOut) — those are Application-layer checks in AttendanceService,
/// mirroring every other cross-cutting business rule in this module; Domain only guards that
/// the mutation methods themselves apply the values given to them.
/// </summary>
internal class Attendance : Entity
{
    public Guid EmployeeId { get; private set; }
    public DateOnly AttendanceDate { get; private set; }
    public DateTime? CheckInTime { get; private set; }
    public DateTime? CheckOutTime { get; private set; }
    public AttendanceStatus Status { get; private set; }
    public string? Remarks { get; private set; }

    // Required by EF Core materialization.
    private Attendance()
    {
    }

    private Attendance(
        Guid id,
        Guid employeeId,
        DateOnly attendanceDate,
        DateTime? checkInTime,
        DateTime? checkOutTime,
        AttendanceStatus status,
        string? remarks,
        Guid? createdBy)
        : base(id, createdBy)
    {
        EmployeeId = employeeId;
        AttendanceDate = attendanceDate;
        CheckInTime = checkInTime;
        CheckOutTime = checkOutTime;
        Status = status;
        Remarks = remarks;
    }

    public static Attendance Create(
        Guid employeeId,
        DateOnly attendanceDate,
        DateTime? checkInTime,
        DateTime? checkOutTime,
        AttendanceStatus status,
        string? remarks,
        Guid? createdBy)
    {
        // Time-ordered UUID per docs/DatabaseArchitecture.md §4 — same convention every
        // other aggregate root in this codebase uses.
        return new Attendance(
            Guid.CreateVersion7(),
            employeeId,
            attendanceDate,
            checkInTime,
            checkOutTime,
            status,
            remarks?.Trim(),
            createdBy);
    }

    /// <summary>Manual correction (full replace of the mutable fields) — used by the plain
    /// PUT endpoint, e.g. to mark Absent/OnLeave without ever checking in.</summary>
    public void Update(
        DateTime? checkInTime,
        DateTime? checkOutTime,
        AttendanceStatus status,
        string? remarks,
        Guid? updatedBy)
    {
        CheckInTime = checkInTime;
        CheckOutTime = checkOutTime;
        Status = status;
        Remarks = remarks?.Trim();
        MarkUpdated(updatedBy);
    }

    /// <summary>Sets CheckInTime only — AttendanceService is responsible for the
    /// AlreadyCheckedIn guard (this method itself has no invariant against overwriting an
    /// already-set CheckInTime).</summary>
    public void RecordCheckIn(DateTime checkInTime, Guid? updatedBy)
    {
        CheckInTime = checkInTime;
        MarkUpdated(updatedBy);
    }

    /// <summary>Sets CheckOutTime only — AttendanceService is responsible for the
    /// NotCheckedIn/AlreadyCheckedOut guards.</summary>
    public void RecordCheckOut(DateTime checkOutTime, Guid? updatedBy)
    {
        CheckOutTime = checkOutTime;
        MarkUpdated(updatedBy);
    }
}
