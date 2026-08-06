namespace HMS.Modules.Calendar.Application;

/// <summary>
/// Stable, machine-readable error codes for expected Calendar-module failures, per
/// docs/ApiStandards.md §5 — the UI branches on these, not on the message text.
/// </summary>
internal static class CalendarErrorCodes
{
    public const string NotFound = "CALENDAR.NOT_FOUND";

    // DepartmentId doesn't resolve to a real department — checked cross-module via
    // HR's IDepartmentService.GetByIdAsync, mirroring HR's own IUserService checks.
    public const string InvalidDepartment = "CALENDAR.INVALID_DEPARTMENT";

    // Another Holiday event already exists on the same calendar date.
    public const string DuplicateHoliday = "CALENDAR.DUPLICATE_HOLIDAY";

    // NOTE — deliberately no error code here for "Doctor Leave overlaps another
    // approved leave for the same doctor". The Event table has no field identifying
    // which doctor a Doctor Leave event belongs to (Phase 1's approved field list has
    // no DoctorId/StaffId, and no approval-status field either), so that rule cannot be
    // evaluated at all — there is nothing to compare "the same doctor" against. Per
    // explicit instruction, this is documented here as a known limitation rather than
    // resolved by adding an unrequested field to the schema. See EventService's own
    // doc comment for the full explanation.
}
