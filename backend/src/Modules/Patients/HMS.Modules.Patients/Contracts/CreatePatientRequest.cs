namespace HMS.Modules.Patients.Contracts;

/// <summary>
/// One combined submit for a New Patient Registration — creates the patient master
/// record and its first <see cref="PatientRegistrationDetails"/> in a single transaction,
/// per docs/PatientRegistrationModule.md's "one save" registration flow (autosave/wizard
/// gating deferred — see docs/DecisionLog.md's MVP-scope ADR).
/// </summary>
public record CreatePatientRequest
{
    // Section 1 — Patient Identification & Demographics
    public Title Title { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateOnly DateOfBirth { get; init; }
    public Gender Gender { get; init; }
    public BloodGroup? BloodGroup { get; init; }

    // Section 2 — Address
    public string AddressLine1 { get; init; } = string.Empty;
    public string? AddressLine2 { get; init; }
    public string? AddressLine3 { get; init; }
    public string District { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Pincode { get; init; } = string.Empty;

    // Section 3 — Contact Details
    public string PrimaryPhone { get; init; } = string.Empty;
    public string? PrimaryPhoneRelation { get; init; }
    public string? AlternatePhone { get; init; }
    public string? Email { get; init; }
    public string? Profession { get; init; }

    // Section 4 — Emergency Contact
    public string EmergencyContactRelationship { get; init; } = string.Empty;
    public string EmergencyContactName { get; init; } = string.Empty;
    public string EmergencyContactPhone { get; init; } = string.Empty;

    // Section 6 — Allergy Details
    public bool HasKnownAllergy { get; init; }
    public string? AllergyType { get; init; }
    public AllergySeverity? AllergySeverity { get; init; }

    // Section 5 & 7 — Mode of Arrival + Registration/Encounter Details
    public PatientRegistrationDetails Registration { get; init; } = new();
}

public record PatientRegistrationDetails
{
    public EncounterType EncounterType { get; init; }
    public ModeOfArrival ModeOfArrival { get; init; }
    public string Department { get; init; } = string.Empty;
    public string Consultant { get; init; } = string.Empty;
    public AdmissionType? AdmissionType { get; init; }
    public string? ReferralSource { get; init; }
    public string? Category { get; init; }
}
