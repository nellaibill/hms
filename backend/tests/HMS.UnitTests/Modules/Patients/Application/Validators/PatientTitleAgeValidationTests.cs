using FluentAssertions;
using FluentValidation.TestHelper;
using HMS.Modules.Patients.Application.Validators;
using HMS.Modules.Patients.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Application.Validators;

public class PatientTitleAgeValidationTests
{
    private static DateOnly AgeYears(int years) => DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-years);

    private static CreatePatientRequest ValidCreateRequest(Title title, DateOnly dateOfBirth) => new()
    {
        Title = title,
        FirstName = "John",
        LastName = "Doe",
        DateOfBirth = dateOfBirth,
        Gender = Gender.Male,
        AddressLine1 = "123 Main St",
        District = "Central",
        State = "State",
        Pincode = "560001",
        PrimaryPhone = "9876543210",
        EmergencyContactRelationship = "Spouse",
        EmergencyContactName = "Jane Doe",
        EmergencyContactPhone = "9876500000",
        Registration = new PatientRegistrationDetails
        {
            EncounterType = EncounterType.OP,
            ModeOfArrival = ModeOfArrival.WalkIn,
            DepartmentId = Guid.NewGuid(),
            ConsultantId = Guid.NewGuid(),
        },
    };

    [Theory]
    [InlineData(Title.Baby, 0, true)]
    [InlineData(Title.Baby, 1, true)]
    [InlineData(Title.Baby, 2, false)]
    [InlineData(Title.Master, 1, true)]
    [InlineData(Title.Master, 17, true)]
    [InlineData(Title.Master, 18, false)]
    [InlineData(Title.Miss, 17, true)]
    [InlineData(Title.Miss, 18, false)]
    [InlineData(Title.Mr, 18, true)]
    [InlineData(Title.Mr, 17, false)]
    [InlineData(Title.Mrs, 30, true)]
    [InlineData(Title.Ms, 17, false)]
    [InlineData(Title.Dr, 25, true)]
    public void IsTitleConsistentWithAge_MatchesExpectedBucket(Title title, int age, bool expected)
    {
        CreatePatientRequestValidator.IsTitleConsistentWithAge(title, AgeYears(age)).Should().Be(expected);
    }

    [Fact]
    public void CreateValidator_RejectsOneDayOldRegisteredAsMr()
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(Title.Mr, DateOnly.FromDateTime(DateTime.UtcNow));

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Title");
    }

    [Fact]
    public void CreateValidator_AcceptsOneDayOldRegisteredAsBaby()
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(Title.Baby, DateOnly.FromDateTime(DateTime.UtcNow));

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor("Title");
    }

    [Fact]
    public void UpdateValidator_RejectsAdultRegisteredAsMaster()
    {
        var validator = new UpdatePatientRequestValidator();
        var request = new UpdatePatientRequest
        {
            Title = Title.Master,
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = AgeYears(30),
            Gender = Gender.Male,
            AddressLine1 = "123 Main St",
            District = "Central",
            State = "State",
            Pincode = "560001",
            PrimaryPhone = "9876543210",
            EmergencyContactRelationship = "Spouse",
            EmergencyContactName = "Jane Doe",
            EmergencyContactPhone = "9876500000",
        };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Title");
    }

    [Theory]
    [InlineData(Title.Mr, Gender.Male, true)]
    [InlineData(Title.Mr, Gender.Female, false)]
    [InlineData(Title.Mr, Gender.Transgender, true)]
    [InlineData(Title.Mr, Gender.NA, true)]
    [InlineData(Title.Mrs, Gender.Female, true)]
    [InlineData(Title.Mrs, Gender.Male, false)]
    [InlineData(Title.Ms, Gender.Female, true)]
    [InlineData(Title.Ms, Gender.Male, false)]
    [InlineData(Title.Miss, Gender.Female, true)]
    [InlineData(Title.Miss, Gender.Male, false)]
    [InlineData(Title.Master, Gender.Male, true)]
    [InlineData(Title.Master, Gender.Female, false)]
    [InlineData(Title.Dr, Gender.Male, true)]
    [InlineData(Title.Dr, Gender.Female, true)]
    [InlineData(Title.Dr, Gender.Transgender, true)]
    [InlineData(Title.Dr, Gender.NA, true)]
    [InlineData(Title.Baby, Gender.Male, true)]
    [InlineData(Title.Baby, Gender.Female, true)]
    [InlineData(Title.Baby, Gender.Transgender, true)]
    [InlineData(Title.Baby, Gender.NA, true)]
    public void IsTitleConsistentWithGender_MatchesExpectedPairing(Title title, Gender gender, bool expected)
    {
        CreatePatientRequestValidator.IsTitleConsistentWithGender(title, gender).Should().Be(expected);
    }

    [Fact]
    public void CreateValidator_RejectsMsWithMaleGender()
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(Title.Ms, AgeYears(30));
        request = request with { Gender = Gender.Male };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Title");
    }

    [Fact]
    public void CreateValidator_AcceptsDrWithAnyGender()
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(Title.Dr, AgeYears(30));
        request = request with { Gender = Gender.Female };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor("Title");
    }
}
