namespace HMS.Modules.IPD.Application;

internal static class IPDErrorCodes
{
    public const string NotFound = "IPD.NOT_FOUND";
    public const string DuplicateCode = "IPD.DUPLICATE_CODE";
    public const string InvalidDepartment = "IPD.INVALID_DEPARTMENT";
    public const string InvalidConsultant = "IPD.INVALID_CONSULTANT";
    public const string InvalidPatient = "IPD.INVALID_PATIENT";
    public const string InvalidWard = "IPD.INVALID_WARD";
    public const string InvalidBed = "IPD.INVALID_BED";
    public const string DuplicateBedNumber = "IPD.DUPLICATE_BED_NUMBER";
    public const string BedNotAvailable = "IPD.BED_NOT_AVAILABLE";
    public const string BedOccupied = "IPD.BED_OCCUPIED";
    public const string PatientAlreadyAdmitted = "IPD.PATIENT_ALREADY_ADMITTED";
    public const string AdmissionAlreadyDischarged = "IPD.ADMISSION_ALREADY_DISCHARGED";
    public const string InvalidDischargeDate = "IPD.INVALID_DISCHARGE_DATE";
}
