namespace HMS.Modules.IPD.Contracts;

public enum WardType
{
    General,
    SemiPrivate,
    Private,
    ICU,
}

public enum BedStatus
{
    Available,
    Occupied,
    Maintenance,
}

public enum BedType
{
    Standard,
    Electric,
    ICU,
    SemiICU,
    Deluxe,
}

public enum AdmissionType
{
    Emergency,
    Elective,
    Transfer,
}

public enum AdmissionStatus
{
    Admitted,
    Discharged,
}

public enum DischargeType
{
    Normal,
    AgainstMedicalAdvice,
    Referred,
}

public enum ChargeType
{
    AdmissionCharge,
    BedCharge,
    NursingCharge,
}
