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
}
