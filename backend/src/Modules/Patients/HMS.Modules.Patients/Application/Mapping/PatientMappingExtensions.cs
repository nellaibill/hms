using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;

namespace HMS.Modules.Patients.Application.Mapping;

/// <summary>
/// Manual entity-to-DTO mapping — two entities don't yet justify a mapping library
/// (Mapster/AutoMapper); see docs/DecisionLog.md ADR-003 (Identity module) for the
/// threshold at which that changes.
/// </summary>
internal static class PatientMappingExtensions
{
    public static PatientResponse ToResponse(this Patient patient)
    {
        var current = patient.Registrations
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

        return new PatientResponse
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
            AddressLine1 = patient.AddressLine1,
            AddressLine2 = patient.AddressLine2,
            AddressLine3 = patient.AddressLine3,
            District = patient.District,
            State = patient.State,
            Pincode = patient.Pincode,
            PrimaryPhone = patient.PrimaryPhone,
            PrimaryPhoneRelation = patient.PrimaryPhoneRelation,
            AlternatePhone = patient.AlternatePhone,
            AlternatePhoneRelation = patient.AlternatePhoneRelation,
            AlternatePhone2 = patient.AlternatePhone2,
            AlternatePhone2Relation = patient.AlternatePhone2Relation,
            Email = patient.Email,
            Profession = patient.Profession,
            EmergencyContactRelationship = patient.EmergencyContactRelationship,
            EmergencyContactName = patient.EmergencyContactName,
            EmergencyContactPhone = patient.EmergencyContactPhone,
            HasKnownAllergy = patient.HasKnownAllergy,
            AllergyType = patient.AllergyType,
            AllergySeverity = patient.AllergySeverity,
            PhotoPath = patient.PhotoPath,
            IdProofType = patient.IdProofType,
            IdProofPath = patient.IdProofPath,
            CurrentRegistration = current?.ToResponse(),
            CreatedAt = patient.CreatedAt,
            UpdatedAt = patient.UpdatedAt,
        };
    }

    public static PatientRegistrationResponse ToResponse(this PatientRegistration registration) => new()
    {
        Id = registration.Id,
        RegistrationNumber = registration.RegistrationNumber,
        EncounterType = registration.EncounterType,
        ModeOfArrival = registration.ModeOfArrival,
        DepartmentId = registration.DepartmentId,
        ConsultantId = registration.ConsultantId,
        AdmissionType = registration.AdmissionType,
        ReferralSource = registration.ReferralSource,
        Category = registration.Category,
        CreatedAt = registration.CreatedAt,
    };
}
