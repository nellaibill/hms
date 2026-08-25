using FluentAssertions;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Domain;

public class PatientTests
{
    private static readonly Guid StateId = Guid.NewGuid();
    private static readonly Guid DistrictId = Guid.NewGuid();

    private static Patient NewPatient(Guid? createdBy = null, DateOnly? dateOfBirth = null) => Patient.Create(
        uhid: "P-2026-000001",
        title: Title.Mr,
        firstName: "John",
        lastName: "Doe",
        dateOfBirth: dateOfBirth ?? new DateOnly(1990, 1, 1),
        gender: Gender.Male,
        bloodGroup: BloodGroup.OPositive,
        maritalStatus: MaritalStatus.Married,
        primaryPhone: "9876543210",
        secondaryPhone: null,
        email: "John.Doe@Example.com",
        profession: null,
        idProofType: null,
        idProofNumber: null,
        modeOfArrivalSource: ModeOfArrivalSource.DoctorReferral,
        modeOfArrivalChannel: null,
        modeOfArrivalSpecify: null,
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
        patient.MaritalStatus.Should().Be(MaritalStatus.Married);
        patient.IsDeleted.Should().BeFalse();
        patient.CreatedBy.Should().Be(actorId);
        patient.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        patient.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_TrimsNamesAndLowercasesEmail()
    {
        var patient = Patient.Create(
            "P-2026-000002", Title.Ms, "  Alice  ", "  Smith  ", new DateOnly(1995, 5, 5), Gender.Female, BloodGroup.Unknown, MaritalStatus.Unmarried,
            "9998887777", null, "  Alice.Smith@Example.com  ", null, null, null, ModeOfArrivalSource.DoctorReferral, null, null, createdBy: null);

        patient.FirstName.Should().Be("Alice");
        patient.LastName.Should().Be("Smith");
        patient.Email.Should().Be("alice.smith@example.com");
    }

    [Fact]
    public void Create_WithNullOrWhitespaceFirstName_Throws()
    {
        var act = () => Patient.Create(
            "P-2026-000003", Title.Mr, "   ", "Doe", new DateOnly(1990, 1, 1), Gender.Male, BloodGroup.Unknown, MaritalStatus.Married,
            "9876543210", null, null, null, null, null, ModeOfArrivalSource.DoctorReferral, null, null, createdBy: null);

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

        patient.UpdateDemographics(Title.Dr, "Johnny", "Doe", new DateOnly(1991, 2, 2), Gender.Transgender, BloodGroup.ANegative, MaritalStatus.NA, updatedBy);

        patient.Title.Should().Be(Title.Dr);
        patient.FirstName.Should().Be("Johnny");
        patient.Gender.Should().Be(Gender.Transgender);
        patient.BloodGroup.Should().Be(BloodGroup.ANegative);
        patient.MaritalStatus.Should().Be(MaritalStatus.NA);
        patient.UpdatedBy.Should().Be(updatedBy);
        patient.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateContact_LowercasesEmail()
    {
        var patient = NewPatient();

        patient.UpdateContact("9000000000", null, "New.Email@Example.com", null, Guid.NewGuid());

        patient.Email.Should().Be("new.email@example.com");
    }

    [Fact]
    public void UpdateIdProof_SetsTypeAndNumber()
    {
        var patient = NewPatient();

        patient.UpdateIdProof(IdProofType.Aadhaar, "123456789012", Guid.NewGuid());

        patient.IdProofType.Should().Be(IdProofType.Aadhaar);
        patient.IdProofNumber.Should().Be("123456789012");
    }

    [Fact]
    public void UpdateModeOfArrival_SetsSourceChannelAndSpecify()
    {
        var patient = NewPatient();

        patient.UpdateModeOfArrival(ModeOfArrivalSource.OnlineAdvertisement, "Other", "A local health forum", Guid.NewGuid());

        patient.ModeOfArrivalSource.Should().Be(ModeOfArrivalSource.OnlineAdvertisement);
        patient.ModeOfArrivalChannel.Should().Be("Other");
        patient.ModeOfArrivalSpecify.Should().Be("A local health forum");
    }

    [Fact]
    public void SetAddress_AndUpdateAddress_UpdatesFieldsAndTrims()
    {
        var patient = NewPatient();
        patient.SetAddress(Address.Create(patient.Id, "Initial Addr", null, null, StateId, DistrictId, "560001"));

        patient.UpdateAddress("  New Addr  ", "Line 2", null, StateId, DistrictId, "560099", Guid.NewGuid());

        patient.Address.AddressLine1.Should().Be("New Addr");
        patient.Address.Pincode.Should().Be("560099");
        patient.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void AddAllergy_AddsToAllergiesCollection()
    {
        var patient = NewPatient();
        var allergy = Allergy.Create(patient.Id, AllergyType.Drug, "Penicillin", AllergySeverity.Severe);

        patient.AddAllergy(allergy, Guid.NewGuid());

        patient.Allergies.Should().ContainSingle().Which.Should().BeSameAs(allergy);
    }

    [Fact]
    public void RemoveAllergy_WhenPresent_RemovesItAndReturnsTrue()
    {
        var patient = NewPatient();
        var allergy = Allergy.Create(patient.Id, AllergyType.Drug, "Penicillin", AllergySeverity.Severe);
        patient.AddAllergy(allergy, null);

        var removed = patient.RemoveAllergy(allergy.Id, Guid.NewGuid());

        removed.Should().BeTrue();
        patient.Allergies.Should().BeEmpty();
    }

    [Fact]
    public void RemoveAllergy_WhenNotPresent_ReturnsFalse()
    {
        var patient = NewPatient();

        var removed = patient.RemoveAllergy(Guid.NewGuid(), Guid.NewGuid());

        removed.Should().BeFalse();
    }

    [Fact]
    public void AddEmergencyContact_AddsToCollection()
    {
        var patient = NewPatient();
        var contact = EmergencyContact.Create(patient.Id, Relationship.Spouse, "Jane Doe", "9876500000");

        patient.AddEmergencyContact(contact, Guid.NewGuid());

        patient.EmergencyContacts.Should().ContainSingle().Which.Should().BeSameAs(contact);
    }

    [Fact]
    public void RemoveEmergencyContact_WhenMoreThanOneRemains_RemovesItAndReturnsTrue()
    {
        var patient = NewPatient();
        var first = EmergencyContact.Create(patient.Id, Relationship.Spouse, "Jane Doe", "9876500000");
        var second = EmergencyContact.Create(patient.Id, Relationship.Friend, "Sam", "9123456780");
        patient.AddEmergencyContact(first, null);
        patient.AddEmergencyContact(second, null);

        var removed = patient.RemoveEmergencyContact(second.Id, Guid.NewGuid());

        removed.Should().BeTrue();
        patient.EmergencyContacts.Should().ContainSingle().Which.Should().BeSameAs(first);
    }

    [Fact]
    public void RemoveEmergencyContact_WhenItIsTheOnlyOne_ReturnsFalseAndDoesNotRemoveIt()
    {
        var patient = NewPatient();
        var onlyContact = EmergencyContact.Create(patient.Id, Relationship.Spouse, "Jane Doe", "9876500000");
        patient.AddEmergencyContact(onlyContact, null);

        var removed = patient.RemoveEmergencyContact(onlyContact.Id, Guid.NewGuid());

        removed.Should().BeFalse();
        patient.EmergencyContacts.Should().ContainSingle();
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
