using FluentAssertions;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Domain;

public class PatientTests
{
    private static Patient NewPatient(Guid? createdBy = null, DateOnly? dateOfBirth = null) => Patient.Create(
        uhid: "P-2026-000001",
        title: Title.Mr,
        firstName: "John",
        lastName: "Doe",
        dateOfBirth: dateOfBirth ?? new DateOnly(1990, 1, 1),
        gender: Gender.Male,
        bloodGroup: BloodGroup.OPositive,
        addressLine1: "123 Main St",
        addressLine2: null,
        addressLine3: null,
        district: "Central",
        state: "State",
        pincode: "560001",
        primaryPhone: "9876543210",
        primaryPhoneRelation: null,
        alternatePhone: null,
        email: "John.Doe@Example.com",
        profession: null,
        emergencyContactRelationship: "Spouse",
        emergencyContactName: "Jane Doe",
        emergencyContactPhone: "9876500000",
        hasKnownAllergy: false,
        allergyType: null,
        allergySeverity: null,
        createdBy: createdBy);

    [Fact]
    public void Create_SetsFieldsAndCreatedAudit()
    {
        var actorId = Guid.NewGuid();

        var patient = NewPatient(actorId);

        patient.Uhid.Should().Be("P-2026-000001");
        patient.FirstName.Should().Be("John");
        patient.LastName.Should().Be("Doe");
        patient.Gender.Should().Be(Gender.Male);
        patient.BloodGroup.Should().Be(BloodGroup.OPositive);
        patient.IsDeleted.Should().BeFalse();
        patient.CreatedBy.Should().Be(actorId);
        patient.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        patient.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_TrimsNamesAndLowercasesEmail()
    {
        var patient = Patient.Create(
            "P-2026-000002", Title.Ms, "  Alice  ", "  Smith  ", new DateOnly(1995, 5, 5), Gender.Female, null,
            "  1 Road  ", null, null, "District", "State", "560002",
            "9998887777", null, null, "  Alice.Smith@Example.com  ", null,
            "Friend", "  Bob  ", "9990001111", false, null, null, null);

        patient.FirstName.Should().Be("Alice");
        patient.LastName.Should().Be("Smith");
        patient.AddressLine1.Should().Be("1 Road");
        patient.Email.Should().Be("alice.smith@example.com");
        patient.EmergencyContactName.Should().Be("Bob");
    }

    [Fact]
    public void Create_WithNullOrWhitespaceFirstName_Throws()
    {
        var act = () => Patient.Create(
            "P-2026-000003", Title.Mr, "   ", "Doe", new DateOnly(1990, 1, 1), Gender.Male, null,
            "Addr", null, null, "District", "State", "560001",
            "9876543210", null, null, null, null,
            "Spouse", "Jane", "9876500000", false, null, null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Age_IsDerivedFromDateOfBirth_NotStored()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var eighteenYearsAgoButOneDayShort = today.AddYears(-18).AddDays(1);

        var patient = NewPatient(dateOfBirth: eighteenYearsAgoButOneDayShort);

        // Birthday hasn't happened yet this year, so the patient is still 17, not 18 —
        // proves Age accounts for the day, not just the year difference.
        patient.Age.Should().Be(17);
    }

    [Fact]
    public void UpdateDemographics_UpdatesFieldsAndSetsUpdatedAudit()
    {
        var patient = NewPatient();
        var updatedBy = Guid.NewGuid();

        patient.UpdateDemographics(Title.Dr, "Johnny", "Doe", new DateOnly(1991, 2, 2), Gender.Other, BloodGroup.ANegative, updatedBy);

        patient.Title.Should().Be(Title.Dr);
        patient.FirstName.Should().Be("Johnny");
        patient.Gender.Should().Be(Gender.Other);
        patient.BloodGroup.Should().Be(BloodGroup.ANegative);
        patient.UpdatedBy.Should().Be(updatedBy);
        patient.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateAddress_UpdatesFieldsAndTrims()
    {
        var patient = NewPatient();

        patient.UpdateAddress("  New Addr  ", "Line 2", null, "New District", "New State", "560099", Guid.NewGuid());

        patient.AddressLine1.Should().Be("New Addr");
        patient.District.Should().Be("New District");
        patient.Pincode.Should().Be("560099");
    }

    [Fact]
    public void UpdateContact_LowercasesEmail()
    {
        var patient = NewPatient();

        patient.UpdateContact("9000000000", null, null, "New.Email@Example.com", null, Guid.NewGuid());

        patient.Email.Should().Be("new.email@example.com");
    }

    [Fact]
    public void UpdateEmergencyContact_UpdatesFields()
    {
        var patient = NewPatient();

        patient.UpdateEmergencyContact("Parent", "New Contact", "9001112222", Guid.NewGuid());

        patient.EmergencyContactRelationship.Should().Be("Parent");
        patient.EmergencyContactName.Should().Be("New Contact");
        patient.EmergencyContactPhone.Should().Be("9001112222");
    }

    [Fact]
    public void UpdateAllergyDetails_WhenHasKnownAllergyIsFalse_ClearsTypeAndSeverity()
    {
        var patient = NewPatient();
        patient.UpdateAllergyDetails(true, "Penicillin", AllergySeverity.Severe, Guid.NewGuid());

        patient.UpdateAllergyDetails(false, "Penicillin", AllergySeverity.Severe, Guid.NewGuid());

        patient.HasKnownAllergy.Should().BeFalse();
        patient.AllergyType.Should().BeNull();
        patient.AllergySeverity.Should().BeNull();
    }

    [Fact]
    public void UpdateAllergyDetails_WhenHasKnownAllergyIsTrue_SetsTypeAndSeverity()
    {
        var patient = NewPatient();

        patient.UpdateAllergyDetails(true, "Peanuts", AllergySeverity.Moderate, Guid.NewGuid());

        patient.HasKnownAllergy.Should().BeTrue();
        patient.AllergyType.Should().Be("Peanuts");
        patient.AllergySeverity.Should().Be(AllergySeverity.Moderate);
    }

    [Fact]
    public void SetPhoto_SetsPathAndUpdatedAudit()
    {
        var patient = NewPatient();
        var updatedBy = Guid.NewGuid();

        patient.SetPhoto("uploads/patients/x/photo/y.jpg", updatedBy);

        patient.PhotoPath.Should().Be("uploads/patients/x/photo/y.jpg");
        patient.UpdatedBy.Should().Be(updatedBy);
    }

    [Fact]
    public void SetIdProof_SetsTypeAndPath()
    {
        var patient = NewPatient();

        patient.SetIdProof(IdProofType.Aadhaar, "uploads/patients/x/id-proof/y.pdf", Guid.NewGuid());

        patient.IdProofType.Should().Be(IdProofType.Aadhaar);
        patient.IdProofPath.Should().Be("uploads/patients/x/id-proof/y.pdf");
    }

    [Fact]
    public void AddRegistration_AddsToRegistrationsCollection()
    {
        var patient = NewPatient();
        var registration = PatientRegistration.Create(patient.Id, "OP-2026-000001", EncounterType.OP, ModeOfArrival.WalkIn, "General Medicine", "Dr. Smith", null, null, null, null);

        patient.AddRegistration(registration);

        patient.Registrations.Should().ContainSingle().Which.Should().BeSameAs(registration);
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedAndDeletedAudit()
    {
        var patient = NewPatient();
        var deletedBy = Guid.NewGuid();

        patient.SoftDelete(deletedBy);

        patient.IsDeleted.Should().BeTrue();
        patient.DeletedBy.Should().Be(deletedBy);
        patient.DeletedAt.Should().NotBeNull();
    }
}
