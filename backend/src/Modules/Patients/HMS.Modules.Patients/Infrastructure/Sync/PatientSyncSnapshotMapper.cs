using System.Text.Json;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;

namespace HMS.Modules.Patients.Infrastructure.Sync;

/// <summary>Converts the Patient aggregate to and from the sync snapshot (ADR-133). Output is
/// deterministic: children are ordered by id, so two identical aggregates always serialise to
/// the same JSON.</summary>
internal static class PatientSyncSnapshotMapper
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static PatientSyncSnapshot ToSnapshot(Patient patient) => new()
    {
        Id = patient.Id,
        Uhid = patient.Uhid,
        Title = patient.Title.ToString(),
        FirstName = patient.FirstName,
        LastName = patient.LastName,
        DateOfBirth = patient.DateOfBirth,
        Gender = patient.Gender.ToString(),
        BloodGroup = patient.BloodGroup.ToString(),
        MaritalStatus = patient.MaritalStatus.ToString(),
        PrimaryPhone = patient.PrimaryPhone,
        SecondaryPhone = patient.SecondaryPhone,
        Email = patient.Email,
        Profession = patient.Profession,
        IdProofType = patient.IdProofType?.ToString(),
        IdProofNumber = patient.IdProofNumber,
        ModeOfArrivalSource = patient.ModeOfArrivalSource.ToString(),
        ModeOfArrivalChannel = patient.ModeOfArrivalChannel,
        ModeOfArrivalSpecify = patient.ModeOfArrivalSpecify,
        RequiresDataVerification = patient.RequiresDataVerification,
        CreatedAt = patient.CreatedAt,
        CreatedBy = patient.CreatedBy,
        UpdatedAt = patient.UpdatedAt,
        UpdatedBy = patient.UpdatedBy,
        IsDeleted = patient.IsDeleted,
        DeletedAt = patient.DeletedAt,
        DeletedBy = patient.DeletedBy,
        Address = patient.Address is null
            ? null
            : new PatientSyncAddress
            {
                AddressLine1 = patient.Address.AddressLine1,
                AddressLine2 = patient.Address.AddressLine2,
                AddressLine3 = patient.Address.AddressLine3,
                StateId = patient.Address.StateId,
                DistrictId = patient.Address.DistrictId,
                Pincode = patient.Address.Pincode,
            },
        Allergies = patient.Allergies
            .OrderBy(a => a.Id)
            .Select(a => new PatientSyncAllergy
            {
                Id = a.Id,
                AllergyType = a.AllergyType.ToString(),
                Specify = a.Specify,
                Severity = a.Severity.ToString(),
            })
            .ToList(),
        EmergencyContacts = patient.EmergencyContacts
            .OrderBy(c => c.Id)
            .Select(c => new PatientSyncEmergencyContact
            {
                Id = c.Id,
                Relationship = c.Relationship.ToString(),
                Name = c.Name,
                Phone = c.Phone,
            })
            .ToList(),
    };

    public static string Serialize(PatientSyncSnapshot snapshot) => JsonSerializer.Serialize(snapshot, JsonOptions);

    /// <summary>Parses and type-checks a payload. Never echoes payload values in the message.</summary>
    public static PatientSyncApplyResult TryParse(string payloadJson, out PatientReplicaState? state)
    {
        state = null;

        PatientSyncSnapshot? snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<PatientSyncSnapshot>(payloadJson, JsonOptions);
        }
        catch (JsonException)
        {
            return Invalid("The payload is not a valid patient snapshot.");
        }

        if (snapshot is null || snapshot.Id == Guid.Empty)
        {
            return Invalid("The payload has no patient id.");
        }

        if (snapshot.Address is null)
        {
            return Invalid("The payload has no address.");
        }

        if (!TryParseEnum<Title>(snapshot.Title, out var title)
            || !TryParseEnum<Gender>(snapshot.Gender, out var gender)
            || !TryParseEnum<BloodGroup>(snapshot.BloodGroup, out var bloodGroup)
            || !TryParseEnum<MaritalStatus>(snapshot.MaritalStatus, out var maritalStatus)
            || !TryParseEnum<ModeOfArrivalSource>(snapshot.ModeOfArrivalSource, out var modeOfArrivalSource))
        {
            return Invalid("The payload has an unknown title, gender, blood group, marital status or mode of arrival.");
        }

        IdProofType? idProofType = null;
        if (snapshot.IdProofType is not null)
        {
            if (!TryParseEnum<IdProofType>(snapshot.IdProofType, out var parsed))
            {
                return Invalid("The payload has an unknown ID proof type.");
            }

            idProofType = parsed;
        }

        var allergies = new List<AllergyReplicaState>(snapshot.Allergies.Count);
        foreach (var allergy in snapshot.Allergies)
        {
            if (allergy.Id == Guid.Empty
                || !TryParseEnum<AllergyType>(allergy.AllergyType, out var allergyType)
                || !TryParseEnum<AllergySeverity>(allergy.Severity, out var severity))
            {
                return Invalid("The payload has an invalid allergy.");
            }

            allergies.Add(new AllergyReplicaState(allergy.Id, allergyType, allergy.Specify, severity));
        }

        var contacts = new List<EmergencyContactReplicaState>(snapshot.EmergencyContacts.Count);
        foreach (var contact in snapshot.EmergencyContacts)
        {
            if (contact.Id == Guid.Empty || !TryParseEnum<Relationship>(contact.Relationship, out var relationship))
            {
                return Invalid("The payload has an invalid emergency contact.");
            }

            contacts.Add(new EmergencyContactReplicaState(contact.Id, relationship, contact.Name, contact.Phone));
        }

        if (allergies.Select(a => a.Id).Distinct().Count() != allergies.Count
            || contacts.Select(c => c.Id).Distinct().Count() != contacts.Count)
        {
            return Invalid("The payload repeats an allergy or emergency contact id.");
        }

        var address = snapshot.Address;
        state = new PatientReplicaState(
            snapshot.Id,
            snapshot.Uhid,
            title,
            snapshot.FirstName,
            snapshot.LastName,
            snapshot.DateOfBirth,
            gender,
            bloodGroup,
            maritalStatus,
            snapshot.PrimaryPhone,
            snapshot.SecondaryPhone,
            snapshot.Email,
            snapshot.Profession,
            idProofType,
            snapshot.IdProofNumber,
            modeOfArrivalSource,
            snapshot.ModeOfArrivalChannel,
            snapshot.ModeOfArrivalSpecify,
            snapshot.RequiresDataVerification,
            DateTime.SpecifyKind(snapshot.CreatedAt, DateTimeKind.Utc),
            snapshot.CreatedBy,
            AsUtc(snapshot.UpdatedAt),
            snapshot.UpdatedBy,
            snapshot.IsDeleted,
            AsUtc(snapshot.DeletedAt),
            snapshot.DeletedBy,
            new AddressReplicaState(address.AddressLine1, address.AddressLine2, address.AddressLine3, address.StateId, address.DistrictId, address.Pincode),
            allergies,
            contacts);

        return PatientSyncApplyResult.Success();
    }

    private static DateTime? AsUtc(DateTime? value) => value is { } v ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : null;

    // Enum.TryParse also accepts numeric strings ("7"), so the parsed value must be a defined
    // member too.
    private static bool TryParseEnum<TEnum>(string? value, out TEnum result)
        where TEnum : struct, Enum
        => Enum.TryParse(value, ignoreCase: false, out result) && Enum.IsDefined(result) && !int.TryParse(value, out _);

    private static PatientSyncApplyResult Invalid(string message) =>
        PatientSyncApplyResult.Rejected(PatientSyncErrorCodes.InvalidPayload, message);
}
