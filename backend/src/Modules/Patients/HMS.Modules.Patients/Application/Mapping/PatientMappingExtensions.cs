using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;

namespace HMS.Modules.Patients.Application.Mapping;

/// <summary>
/// Manual entity-to-DTO mapping — the aggregate isn't big enough yet to justify a mapping
/// library (Mapster/AutoMapper).
/// </summary>
internal static class PatientMappingExtensions
{
    public static PatientResponse ToResponse(this Patient patient, string rowVersion) => new()
    {
        Id = patient.Id,
        Uhid = patient.Uhid,
        Title = patient.Title,
        FirstName = patient.FirstName,
        LastName = patient.LastName,
        DateOfBirth = patient.DateOfBirth,
        Age = patient.Age,
        Gender = patient.Gender,
        BloodGroup = patient.BloodGroup,
        MaritalStatus = patient.MaritalStatus,
        PrimaryPhone = patient.PrimaryPhone,
        SecondaryPhone = patient.SecondaryPhone,
        Email = patient.Email,
        Profession = patient.Profession,
        IdProofType = patient.IdProofType,
        IdProofNumber = patient.IdProofNumber,
        ModeOfArrivalSource = patient.ModeOfArrivalSource,
        ModeOfArrivalChannel = patient.ModeOfArrivalChannel,
        ModeOfArrivalSpecify = patient.ModeOfArrivalSpecify,
        Address = patient.Address.ToResponse(),
        Allergies = patient.Allergies.Select(a => a.ToResponse()).ToList(),
        EmergencyContacts = patient.EmergencyContacts.Select(c => c.ToResponse()).ToList(),
        RequiresDataVerification = patient.RequiresDataVerification,
        RowVersion = rowVersion,
        CreatedAt = patient.CreatedAt,
        UpdatedAt = patient.UpdatedAt,
        UpdatedBy = patient.UpdatedBy,
    };

    public static AddressResponse ToResponse(this Address address) => new()
    {
        AddressLine1 = address.AddressLine1,
        AddressLine2 = address.AddressLine2,
        AddressLine3 = address.AddressLine3,
        StateId = address.StateId,
        DistrictId = address.DistrictId,
        Pincode = address.Pincode,
    };

    public static AllergyResponse ToResponse(this Allergy allergy) => new()
    {
        Id = allergy.Id,
        AllergyType = allergy.AllergyType,
        Specify = allergy.Specify,
        Severity = allergy.Severity,
    };

    public static EmergencyContactResponse ToResponse(this EmergencyContact contact) => new()
    {
        Id = contact.Id,
        Relationship = contact.Relationship,
        Name = contact.Name,
        Phone = contact.Phone,
    };
}
