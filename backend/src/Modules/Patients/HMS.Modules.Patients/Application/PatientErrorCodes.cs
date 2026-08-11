namespace HMS.Modules.Patients.Application;

/// <summary>
/// Stable, machine-readable error codes for expected Patients-module failures, per
/// docs/ApiStandards.md §5 — the UI branches on these, not on the message text.
/// </summary>
internal static class PatientErrorCodes
{
    public const string NotFound = "PATIENTS.PATIENT_NOT_FOUND";
    public const string InvalidFile = "PATIENTS.INVALID_FILE";
    public const string DuplicatePatient = "PATIENTS.DUPLICATE_PATIENT";
    public const string InvalidDepartment = "PATIENTS.INVALID_DEPARTMENT";
    public const string InvalidConsultant = "PATIENTS.INVALID_CONSULTANT";
    public const string InvalidAppointmentType = "PATIENTS.INVALID_APPOINTMENT_TYPE";
    public const string ConcurrencyConflict = "PATIENTS.CONCURRENCY_CONFLICT";
}
