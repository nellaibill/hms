namespace HMS.Modules.Patients.Domain;

/// <summary>
/// A patient's address — a true 1:1 extension of <see cref="Patient"/>: PatientId is both
/// this entity's primary key and its foreign key (see Infrastructure/Configurations/
/// AddressConfiguration.cs), so a patient can never have more than one address row and an
/// address can never exist without its patient. StateId/DistrictId are references into
/// Masters' own State/District tables (validated in PatientService, not here — Domain never
/// depends on another module).
/// </summary>
/// <remarks>
/// Whether Address line 1, State, District and Pincode are mandatory is a per-hospital
/// setting (ADR-135), enforced by the request validators — so this entity accepts them
/// blank. A blank Address line 1 or Pincode is stored as an empty string (those columns stay
/// NOT NULL); a missing State or District is stored as null.
/// </remarks>
internal class Address
{
    public Guid PatientId { get; private set; }

    public string AddressLine1 { get; private set; } = string.Empty;
    public string? AddressLine2 { get; private set; }
    public string? AddressLine3 { get; private set; }
    public Guid? StateId { get; private set; }
    public Guid? DistrictId { get; private set; }
    public string Pincode { get; private set; } = string.Empty;

    // Required by EF Core materialization.
    private Address()
    {
    }

    private Address(Guid patientId, string addressLine1, string? addressLine2, string? addressLine3, Guid? stateId, Guid? districtId, string pincode)
    {
        PatientId = patientId;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        AddressLine3 = addressLine3;
        StateId = stateId;
        DistrictId = districtId;
        Pincode = pincode;
    }

    public static Address Create(Guid patientId, string? addressLine1, string? addressLine2, string? addressLine3, Guid? stateId, Guid? districtId, string? pincode)
        => new(
            patientId,
            (addressLine1 ?? string.Empty).Trim(),
            addressLine2?.Trim(),
            addressLine3?.Trim(),
            NullIfEmpty(stateId),
            NullIfEmpty(districtId),
            (pincode ?? string.Empty).Trim());

    public void Update(string? addressLine1, string? addressLine2, string? addressLine3, Guid? stateId, Guid? districtId, string? pincode)
    {
        AddressLine1 = (addressLine1 ?? string.Empty).Trim();
        AddressLine2 = addressLine2?.Trim();
        AddressLine3 = addressLine3?.Trim();
        StateId = NullIfEmpty(stateId);
        DistrictId = NullIfEmpty(districtId);
        Pincode = (pincode ?? string.Empty).Trim();
    }

    private static Guid? NullIfEmpty(Guid? id) => id == Guid.Empty ? null : id;
}
