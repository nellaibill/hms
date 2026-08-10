using FluentAssertions;
using FluentValidation.TestHelper;
using HMS.Modules.Patients.Application.Validators;
using HMS.Modules.Patients.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Application.Validators;

public class PatientNamePhoneValidationTests
{
    private static CreatePatientRequest ValidCreateRequest(
        string firstName = "John",
        string lastName = "Doe",
        string primaryPhone = "9876543210",
        string? primaryPhoneRelation = null,
        string? alternatePhone = null,
        string emergencyContactRelationship = "Spouse",
        string emergencyContactName = "Jane Doe",
        string emergencyContactPhone = "9876500000") => new()
    {
        Title = Title.Mr,
        FirstName = firstName,
        LastName = lastName,
        DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-30),
        Gender = Gender.Male,
        AddressLine1 = "123 Main St",
        District = "Central",
        State = "State",
        Pincode = "560001",
        PrimaryPhone = primaryPhone,
        PrimaryPhoneRelation = primaryPhoneRelation,
        AlternatePhone = alternatePhone,
        EmergencyContactRelationship = emergencyContactRelationship,
        EmergencyContactName = emergencyContactName,
        EmergencyContactPhone = emergencyContactPhone,
        Registration = new PatientRegistrationDetails
        {
            EncounterType = EncounterType.OP,
            ModeOfArrival = ModeOfArrival.WalkIn,
            DepartmentId = Guid.NewGuid(),
            ConsultantId = Guid.NewGuid(),
        },
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

        result.ShouldHaveValidationErrorFor(x => x.EmergencyContactName);
    }

    [Fact]
    public void CreateValidator_RejectsDigitsInEmergencyContactRelationship()
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(emergencyContactRelationship: "Sp0use");

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.EmergencyContactRelationship);
    }

    [Fact]
    public void CreateValidator_RejectsDigitsInPrimaryPhoneRelation()
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(primaryPhoneRelation: "Self1");

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.PrimaryPhoneRelation);
    }

    [Theory]
    [InlineData("9876543210", true)]
    [InlineData("+91-98765-43210", true)]
    [InlineData("123", false)]
    [InlineData("12", false)]
    [InlineData("----------", false)]
    public void CreateValidator_PrimaryPhoneMustHaveAtLeastTenDigits(string phone, bool expectedValid)
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
    public void CreateValidator_RejectsShortAlternatePhone()
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(alternatePhone: "999");

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.AlternatePhone);
    }

    [Fact]
    public void CreateValidator_RejectsShortEmergencyContactPhone()
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(emergencyContactPhone: "12345");

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.EmergencyContactPhone);
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
            AddressLine1 = "123 Main St",
            District = "Central",
            State = "State",
            Pincode = "560001",
            PrimaryPhone = "123",
            EmergencyContactRelationship = "Spouse",
            EmergencyContactName = "Jane Doe",
            EmergencyContactPhone = "9876500000",
        };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.LastName);
        result.ShouldHaveValidationErrorFor(x => x.PrimaryPhone);
    }
}
