namespace HMS.Modules.Patients.Contracts;

/// <summary>
/// Updates a patient's core demographic/contact/address/mode-of-arrival fields. Allergies
/// and Emergency Contacts have their own add/remove endpoints (AddAllergyRequest,
/// AddEmergencyContactRequest) rather than being replaced wholesale here — editing one entry
/// shouldn't require resending the whole patient or risk an optimistic-concurrency conflict
/// on unrelated fields.
/// </summary>
public record UpdatePatientRequest
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

    /// <summary>Must echo back the RowVersion from the PatientResponse this edit was loaded
    /// from — lets the server detect and reject a save made against data someone else has
    /// since changed, rather than silently overwriting it. See PatientService.UpdateAsync.</summary>
    public string RowVersion { get; init; } = string.Empty;
}
