namespace HMS.Modules.Patients.Contracts;

public record PatientResponse
{
    public Guid Id { get; init; }
    public string Uhid { get; init; } = string.Empty;

    public Title Title { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateOnly DateOfBirth { get; init; }
    public int Age { get; init; }
    public Gender Gender { get; init; }
    public BloodGroup? BloodGroup { get; init; }

    public string AddressLine1 { get; init; } = string.Empty;
    public string? AddressLine2 { get; init; }
    public string? AddressLine3 { get; init; }
    public string District { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Pincode { get; init; } = string.Empty;

    public string PrimaryPhone { get; init; } = string.Empty;
    public string? PrimaryPhoneRelation { get; init; }
    public string? AlternatePhone { get; init; }
    public string? AlternatePhoneRelation { get; init; }
    public string? AlternatePhone2 { get; init; }
    public string? AlternatePhone2Relation { get; init; }
    public string? Email { get; init; }
    public string? Profession { get; init; }

    public string EmergencyContactRelationship { get; init; } = string.Empty;
    public string EmergencyContactName { get; init; } = string.Empty;
    public string EmergencyContactPhone { get; init; } = string.Empty;

    public bool HasKnownAllergy { get; init; }
    public string? AllergyType { get; init; }
    public AllergySeverity? AllergySeverity { get; init; }

    public string? PhotoPath { get; init; }
    public IdProofType? IdProofType { get; init; }
    public string? IdProofPath { get; init; }

    /// <summary>The most recent registration/encounter — a patient may have several over time.</summary>
    public PatientRegistrationResponse? CurrentRegistration { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record PatientRegistrationResponse
{
    public Guid Id { get; init; }
    public string RegistrationNumber { get; init; } = string.Empty;
    public EncounterType EncounterType { get; init; }
    public ModeOfArrival ModeOfArrival { get; init; }
    public Guid DepartmentId { get; init; }
    public Guid ConsultantId { get; init; }
    public AdmissionType? AdmissionType { get; init; }
    public string? ReferralSource { get; init; }
    public string? Category { get; init; }
    public DateTime CreatedAt { get; init; }
}
