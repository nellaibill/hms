namespace HMS.Modules.Appointments.Application;

/// <summary>
/// Stable, machine-readable error codes for expected Appointments failures, per
/// docs/ApiStandards.md §5 — the UI branches on these, not on the message text.
/// </summary>
internal static class AppointmentErrorCodes
{
    public const string NotFound = "APPOINTMENTS.NOT_FOUND";
    public const string BlockNotFound = "APPOINTMENTS.BLOCK_NOT_FOUND";

    public const string InvalidConsultant = "APPOINTMENTS.INVALID_CONSULTANT";
    public const string ConsultantInactive = "APPOINTMENTS.CONSULTANT_INACTIVE";
    public const string InvalidDepartment = "APPOINTMENTS.INVALID_DEPARTMENT";
    public const string InvalidAppointmentType = "APPOINTMENTS.INVALID_APPOINTMENT_TYPE";
    public const string InvalidPatient = "APPOINTMENTS.INVALID_PATIENT";
    public const string InvalidVisit = "APPOINTMENTS.INVALID_VISIT";
    public const string PatientMismatch = "APPOINTMENTS.PATIENT_MISMATCH";

    public const string DateInPast = "APPOINTMENTS.DATE_IN_PAST";
    public const string TooFarAhead = "APPOINTMENTS.TOO_FAR_AHEAD";
    public const string NotAClinicDay = "APPOINTMENTS.NOT_A_CLINIC_DAY";
    public const string InvalidSlot = "APPOINTMENTS.INVALID_SLOT";
    public const string SlotInPast = "APPOINTMENTS.SLOT_IN_PAST";
    public const string ConsultantUnavailable = "APPOINTMENTS.CONSULTANT_UNAVAILABLE";
    public const string SlotFull = "APPOINTMENTS.SLOT_FULL";
    public const string DuplicateBooking = "APPOINTMENTS.DUPLICATE_BOOKING";

    // The action isn't allowed in the appointment's current status (only Booked can change).
    public const string InvalidStatus = "APPOINTMENTS.INVALID_STATUS";

    // No-show before the appointment's day, or check-in on a day other than the appointment's.
    public const string WrongDay = "APPOINTMENTS.WRONG_DAY";
}
