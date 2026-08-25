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

    public AddressResponse Address { get; init; } = new();
    public IReadOnlyList<AllergyResponse> Allergies { get; init; } = [];
    public IReadOnlyList<EmergencyContactResponse> EmergencyContacts { get; init; } = [];

    /// <summary>Opaque optimistic-concurrency token (the row's Postgres xmin at read time) —
    /// echo this back on UpdatePatientRequest.RowVersion so a save made against stale data is
    /// rejected with a clear conflict instead of silently overwriting someone else's edit.</summary>
    public string RowVersion { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record AddressResponse
{
    public string AddressLine1 { get; init; } = string.Empty;
    public string? AddressLine2 { get; init; }
    public string? AddressLine3 { get; init; }
    public Guid StateId { get; init; }
    public Guid DistrictId { get; init; }
    public string Pincode { get; init; } = string.Empty;
}

public record AllergyResponse
{
    public Guid Id { get; init; }
    public AllergyType AllergyType { get; init; }
    public string? Specify { get; init; }
    public AllergySeverity Severity { get; init; }
}

public record EmergencyContactResponse
{
    public Guid Id { get; init; }
    public Relationship Relationship { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
}
