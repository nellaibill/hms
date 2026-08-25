namespace HMS.Modules.Patients.Application;

/// <summary>
/// Stable, machine-readable error codes for expected Patients-module failures — the UI
/// branches on these, not on the message text.
/// </summary>
internal static class PatientErrorCodes
{
    public const string NotFound = "PATIENTS.PATIENT_NOT_FOUND";
    public const string DuplicatePatient = "PATIENTS.DUPLICATE_PATIENT";
    public const string InvalidState = "PATIENTS.INVALID_STATE";
    public const string InvalidDistrict = "PATIENTS.INVALID_DISTRICT";
    public const string ConcurrencyConflict = "PATIENTS.CONCURRENCY_CONFLICT";
    public const string AllergyNotFound = "PATIENTS.ALLERGY_NOT_FOUND";
    public const string EmergencyContactNotFound = "PATIENTS.EMERGENCY_CONTACT_NOT_FOUND";
    public const string CannotRemoveLastEmergencyContact = "PATIENTS.CANNOT_REMOVE_LAST_EMERGENCY_CONTACT";
}
