namespace HMS.Modules.Patients.Contracts;

/// <summary>
/// Shared vocabulary for the Patients module — public because these values cross the
/// HTTP boundary (request/response fields) and Swagger needs to describe them, but also
/// used directly by Domain/Application within this same assembly.
/// </summary>
public enum Title
{
    Mr,
    Mrs,
    Ms,
    Miss,
    Dr,
    Master,
    Baby,
}

public enum Gender
{
    Male,
    Female,
    Transgender,
    NA,
}

public enum BloodGroup
{
    APositive,
    ANegative,
    BPositive,
    BNegative,
    ABPositive,
    ABNegative,
    OPositive,
    ONegative,
    Unknown,
}

/// <summary>Matches the frontend's MARITAL_STATUSES exactly.</summary>
public enum MaritalStatus
{
    Married,
    Unmarried,
    NA,
}

public enum IdProofType
{
    Aadhaar,
    Passport,
    DrivingLicense,
    VoterId,
    Other,
}

/// <summary>Matches the frontend's ALLERGY_CATEGORIES exactly.</summary>
public enum AllergyType
{
    Food,
    Drug,
    Environmental,
    Contact,
    Others,
}

public enum AllergySeverity
{
    Mild,
    Moderate,
    Severe,
}

/// <summary>Matches the frontend's RELATIONSHIPS list exactly (used for Emergency Contacts).</summary>
public enum Relationship
{
    Father,
    Mother,
    Son,
    Daughter,
    Sister,
    Brother,
    Spouse,
    Grandson,
    Granddaughter,
    Grandfather,
    Grandmother,
    Cousin,
    Friend,
    FatherInLaw,
    MotherInLaw,
    SonInLaw,
    DaughterInLaw,
    SisterInLaw,
    BrotherInLaw,
    Other,
}

/// <summary>Matches the frontend's ARRIVAL_SOURCE_CATEGORIES exactly.</summary>
public enum ModeOfArrivalSource
{
    DoctorReferral,
    PatientOrRelativeReferral,
    OnlineAdvertisement,
    OfflineAdvertisement,
}

/// <summary>Matches the frontend's ENCOUNTER_TYPES_UI exactly.</summary>
public enum VisitType
{
    OP,
    IP,
    Emergency,
    DayCare,
    Observation,
}
