namespace HMS.Modules.Patients.Contracts;

/// <summary>
/// One combined submit for a New Patient Registration — creates the patient, their address,
/// and any allergies/emergency contacts supplied up front, all in a single transaction.
/// Encounter/visit registration (department, consultant, admission type) is out of scope
/// for this iteration.
/// </summary>
public record CreatePatientRequest
{
    public Title Title { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateOnly DateOfBirth { get; init; }
    public Gender Gender { get; init; }
    public BloodGroup BloodGroup { get; init; }
    public MaritalStatus MaritalStatus { get; init; }

    public string PrimaryPhone { get; init; } = string.Empty;
    public string? SecondaryPhone { get; init; }
    public string? Email { get; init; }
    public string? Profession { get; init; }

    public IdProofType? IdProofType { get; init; }
    public string? IdProofNumber { get; init; }

    public ModeOfArrivalSource ModeOfArrivalSource { get; init; }
    public string? ModeOfArrivalChannel { get; init; }
    public string? ModeOfArrivalSpecify { get; init; }

    public AddressRequest Address { get; init; } = new();
    public IReadOnlyList<AllergyRequest> Allergies { get; init; } = [];
    public IReadOnlyList<EmergencyContactRequest> EmergencyContacts { get; init; } = [];
}

public record AddressRequest
{
    public string AddressLine1 { get; init; } = string.Empty;
    public string? AddressLine2 { get; init; }
    public string? AddressLine3 { get; init; }
    public Guid StateId { get; init; }
    public Guid DistrictId { get; init; }
    public string Pincode { get; init; } = string.Empty;
}

public record AllergyRequest
{
    public AllergyType AllergyType { get; init; }
    public string? Specify { get; init; }
    public AllergySeverity Severity { get; init; }
}

public record EmergencyContactRequest
{
    public Relationship Relationship { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
}
