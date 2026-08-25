using FluentValidation.TestHelper;
using HMS.Modules.Patients.Application.Validators;
using HMS.Modules.Patients.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Application.Validators;

public class PatientNamePhoneValidationTests
{
    private static readonly Guid StateId = Guid.NewGuid();
    private static readonly Guid DistrictId = Guid.NewGuid();

    private static CreatePatientRequest ValidCreateRequest(
        string firstName = "John",
        string lastName = "Doe",
        string primaryPhone = "9876543210",
        string? secondaryPhone = null,
        string emergencyContactName = "Jane Doe",
        string emergencyContactPhone = "9876500000") => new()
    {
        Title = Title.Mr,
        FirstName = firstName,
        LastName = lastName,
        DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-30),
        Gender = Gender.Male,
        BloodGroup = BloodGroup.Unknown,
        MaritalStatus = MaritalStatus.Married,
        PrimaryPhone = primaryPhone,
        SecondaryPhone = secondaryPhone,
        ModeOfArrivalSource = ModeOfArrivalSource.DoctorReferral,
        Address = new AddressRequest { AddressLine1 = "123 Main St", StateId = StateId, DistrictId = DistrictId, Pincode = "560001" },
        EmergencyContacts = [new EmergencyContactRequest { Relationship = Relationship.Spouse, Name = emergencyContactName, Phone = emergencyContactPhone }],
    };

    [Theory]
    [InlineData("John", true)]
    [InlineData("Mary-Jane", true)]
    [InlineData("O'Brien", true)]
    [InlineData("Dr. Rao", true)]
    [InlineData("John123", false)]
    [InlineData("123", false)]
    [InlineData("", false)]
    public void CreateValidator_FirstNameMustBeLettersOnly(string firstName, bool expectedValid)
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(firstName: firstName);

        var result = validator.TestValidate(request);

        if (expectedValid)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.FirstName);
        }
    }

    [Fact]
    public void CreateValidator_RejectsDigitsInEmergencyContactName()
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(emergencyContactName: "12345");

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("EmergencyContacts[0].Name");
    }

    // Phone numbers are exactly 10 digits now — no country code, no formatting characters.
    [Theory]
    [InlineData("9876543210", true)]
    [InlineData("+91-98765-43210", false)]
    [InlineData("123", false)]
    [InlineData("12", false)]
    [InlineData("98765432101", false)]
    [InlineData("----------", false)]
    public void CreateValidator_PrimaryPhoneMustBeExactlyTenDigits(string phone, bool expectedValid)
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(primaryPhone: phone);

        var result = validator.TestValidate(request);

        if (expectedValid)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.PrimaryPhone);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.PrimaryPhone);
        }
    }

    [Fact]
    public void CreateValidator_RejectsShortSecondaryPhone()
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(secondaryPhone: "999");

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.SecondaryPhone);
    }

    [Fact]
    public void CreateValidator_RejectsShortEmergencyContactPhone()
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(emergencyContactPhone: "12345");

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("EmergencyContacts[0].Phone");
    }

    [Fact]
    public void CreateValidator_RequiresAtLeastOneEmergencyContact()
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest() with { EmergencyContacts = [] };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.EmergencyContacts);
    }

    [Fact]
    public void UpdateValidator_RejectsDigitsInLastNameAndShortPhone()
    {
        var validator = new UpdatePatientRequestValidator();
        var request = new UpdatePatientRequest
        {
            Title = Title.Mr,
            FirstName = "John",
            LastName = "Doe2",
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-30),
            Gender = Gender.Male,
            BloodGroup = BloodGroup.Unknown,
            MaritalStatus = MaritalStatus.Married,
            PrimaryPhone = "123",
            ModeOfArrivalSource = ModeOfArrivalSource.DoctorReferral,
            Address = new AddressRequest { AddressLine1 = "123 Main St", StateId = StateId, DistrictId = DistrictId, Pincode = "560001" },
            RowVersion = "1",
        };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.LastName);
        result.ShouldHaveValidationErrorFor(x => x.PrimaryPhone);
    }

    [Fact]
    public void UpdateValidator_RejectsMissingRowVersion()
    {
        var validator = new UpdatePatientRequestValidator();
        var request = new UpdatePatientRequest
        {
            Title = Title.Mr,
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-30),
            Gender = Gender.Male,
            BloodGroup = BloodGroup.Unknown,
            MaritalStatus = MaritalStatus.Married,
            PrimaryPhone = "9876543210",
            ModeOfArrivalSource = ModeOfArrivalSource.DoctorReferral,
            Address = new AddressRequest { AddressLine1 = "123 Main St", StateId = StateId, DistrictId = DistrictId, Pincode = "560001" },
            RowVersion = "",
        };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.RowVersion);
    }
}
