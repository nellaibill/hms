namespace HMS.Modules.Patients.Contracts;

/// <summary>
/// Shared vocabulary for the Patients module — public because these values cross the
/// HTTP boundary (request/response fields) and Swagger needs to describe them, but also
/// used directly by Domain/Application within this same assembly (see docs/DecisionLog.md
/// for why enums are the one type Domain is allowed to share with Contracts).
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

// Matches the frontend's PATIENT_GENDERS exactly (Male/Female/Transgender/NA) — previously
// this only had Male/Female/Other, so Transgender and "prefer not to say" both collapsed
// into the same "Other" value on save and were indistinguishable again on the next edit.
// Stored as a string column (see PatientConfiguration), so this rename is not a breaking
// schema change — just a wider vocabulary.
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

public enum AllergySeverity
{
    Mild,
    Moderate,
    Severe,
}

public enum IdProofType
{
    Aadhaar,
    Passport,
    DrivingLicense,
    VoterId,
    Other,
}

/// <summary>Per docs/PatientRegistrationModule.md §2 (Form Section 7).</summary>
public enum EncounterType
{
    OP,
    IP,
    Emergency,
    DayCare,
}

/// <summary>Per docs/PatientRegistrationModule.md §2 (Form Section 5).</summary>
public enum ModeOfArrival
{
    WalkIn,
    Ambulance,
    Referred,
}

/// <summary>Admission type only applies to IP/Emergency encounters — see §5's progressive disclosure rule.</summary>
public enum AdmissionType
{
    MLC,
    NMLC,
}
