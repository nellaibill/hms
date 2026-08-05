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
}
