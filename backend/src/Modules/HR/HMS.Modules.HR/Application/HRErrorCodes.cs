namespace HMS.Modules.HR.Application;

/// <summary>
/// Stable, machine-readable error codes for expected HR-module failures, per
/// docs/ApiStandards.md §5 — the UI branches on these, not on the message text. One shared
/// class for the whole module (mirrors HMS.Modules.Masters.MastersErrorCodes) rather than a
/// per-entity {Entity}ErrorCodes class, since this module is expected to grow into several
/// roster-related entities (ShiftAssignment, WeeklyRoster, ...) that will all fail for the
/// same two reasons: not found, or a duplicate business code.
/// </summary>
internal static class HRErrorCodes
{
    public const string NotFound = "HR.NOT_FOUND";
    public const string DuplicateCode = "HR.DUPLICATE_CODE";

    // Mirrors IDENTITY.USER_INVALID_ROLE's role in UserService.CreateAsync: a referenced
    // sibling aggregate (there, Role; here, Shift) doesn't exist.
    public const string InvalidShift = "HR.INVALID_SHIFT";

    // Used by ShiftSwapRequestService for CurrentShiftAssignmentId/RequestedShiftAssignmentId
    // referential validation — application-layer existence checks only, not a database FK.
    public const string InvalidShiftAssignment = "HR.INVALID_SHIFT_ASSIGNMENT";

    // DepartmentId doesn't resolve to a real Department record (ShiftAssignment,
    // WeeklyRoster).
    public const string InvalidDepartment = "HR.INVALID_DEPARTMENT";

    // StaffId (or RequestedByStaffId/RequestedToStaffId/ApprovedBy) doesn't resolve to a
    // real Identity user — checked cross-module via IUserService.GetByIdAsync, since User
    // lives in a different module.
    public const string InvalidStaff = "HR.INVALID_STAFF";

    // WeeklyRoster: another roster already exists for the same Department + WeekStartDate.
    public const string DuplicateRoster = "HR.DUPLICATE_ROSTER";

    // ShiftAssignment: the same staff member already has another assignment on the same
    // RosterDate whose shift time range overlaps this one's.
    public const string ShiftOverlap = "HR.SHIFT_OVERLAP";

    // --- Employee/Attendance/LeaveType/LeaveRequest (Hospital HR Management MVP) ---

    // Employee.DesignationId doesn't resolve to a real Masters.Designation record.
    public const string InvalidDesignation = "HR.INVALID_DESIGNATION";

    // Employee.ReportingManagerId doesn't resolve to a real Employee, or refers to the
    // employee itself (an employee may not report to themselves).
    public const string InvalidReportingManager = "HR.INVALID_REPORTING_MANAGER";

    // Employee.UserId doesn't resolve to a real identity.users row — checked cross-module via
    // Identity's public IUserService.GetByIdAsync only when a UserId is actually supplied
    // (it's always optional).
    public const string InvalidUser = "HR.INVALID_USER";

    // Attendance.EmployeeId / LeaveRequest.EmployeeId doesn't resolve to a real Employee.
    public const string InvalidEmployee = "HR.INVALID_EMPLOYEE";

    // LeaveRequest.LeaveTypeId doesn't resolve to a real LeaveType.
    public const string InvalidLeaveType = "HR.INVALID_LEAVE_TYPE";

    // Attendance: another attendance row already exists for the same Employee + AttendanceDate.
    public const string DuplicateAttendance = "HR.DUPLICATE_ATTENDANCE";

    // Attendance check-in/check-out state machine.
    public const string AlreadyCheckedIn = "HR.ALREADY_CHECKED_IN";
    public const string NotCheckedIn = "HR.NOT_CHECKED_IN";
    public const string AlreadyCheckedOut = "HR.ALREADY_CHECKED_OUT";

    // LeaveRequest: EndDate earlier than StartDate.
    public const string InvalidDateRange = "HR.INVALID_DATE_RANGE";

    // LeaveRequest: Approve/Reject/Cancel attempted from a status other than the one each
    // transition requires (all three only apply to Pending).
    public const string InvalidStatusTransition = "HR.INVALID_STATUS_TRANSITION";
}
