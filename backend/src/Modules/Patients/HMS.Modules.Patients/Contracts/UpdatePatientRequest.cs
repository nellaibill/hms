namespace HMS.Modules.Patients.Contracts;

/// <summary>
/// Updates a patient's demographic/master-data fields only. Editing an existing
/// registration/encounter is out of scope for this iteration — see docs/DecisionLog.md's
/// MVP-scope ADR.
/// </summary>
public record UpdatePatientRequest
{
    public Title Title { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateOnly DateOfBirth { get; init; }
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
}
