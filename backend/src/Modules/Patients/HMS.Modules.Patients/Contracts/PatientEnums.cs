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

/// <summary>Whether a non-OP encounter is a Medico-Legal Case — the "Admission type"
/// (IP/Emergency) / "Observation type" (Day Care/Observation) dropdown. Matches the frontend's
/// ADMISSION_TYPES exactly. Not named AdmissionType: HMS.Modules.IPD.Contracts already has an
/// unrelated enum by that name (Emergency/Elective/Transfer).</summary>
public enum MedicoLegalType
{
    MLC,
    NMLC,
}

/// <summary>Who referred a non-OP patient. Matches the frontend's REFERRAL_COLUMN_CATEGORIES
/// exactly.</summary>
public enum ReferralSource
{
    Ambulance,
    Doctor,
    Medicals,
    Others,
}

/// <summary>Where an Emergency patient is being seen — the "Emergency type" dropdown. Matches
/// the frontend's EMERGENCY_TYPES exactly. LR is the Labour Room.</summary>
public enum EmergencyType
{
    ICU,
    Casualty,
    LR,
}

/// <summary>Placeholder list — the hospital's real visit categories aren't decided yet
/// ("A, B, C as of now"). Matches the frontend's VISIT_CATEGORIES exactly.</summary>
public enum VisitCategory
{
    A,
    B,
    C,
}

/// <summary>The OPD queue status of one consultation line — set at Create (Waiting) and
/// advanced via PatientVisitConsultation's transition methods as the patient moves through
/// the front desk (CheckedIn), a consultant's room (InConsultation), and out
/// (Completed/Cancelled/NoShow).</summary>
public enum OpdConsultationStatus
{
    Waiting,
    CheckedIn,
    InConsultation,
    Completed,
    Cancelled,
    NoShow,
}
