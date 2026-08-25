using FluentAssertions;
using FluentValidation.TestHelper;
using HMS.Modules.Patients.Application.Validators;
using HMS.Modules.Patients.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Application.Validators;

public class PatientMaritalStatusValidationTests
{
    private static readonly Guid StateId = Guid.NewGuid();
    private static readonly Guid DistrictId = Guid.NewGuid();

    private static DateOnly AgeYears(int years) => DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-years);

    private static CreatePatientRequest ValidCreateRequest(MaritalStatus maritalStatus, DateOnly dateOfBirth, Title title = Title.Mr) => new()
    {
        Title = title,
        FirstName = "John",
        LastName = "Doe",
        DateOfBirth = dateOfBirth,
        Gender = Gender.Male,
        BloodGroup = BloodGroup.Unknown,
        MaritalStatus = maritalStatus,
        PrimaryPhone = "9876543210",
        ModeOfArrivalSource = ModeOfArrivalSource.DoctorReferral,
        Address = new AddressRequest { AddressLine1 = "123 Main St", StateId = StateId, DistrictId = DistrictId, Pincode = "560001" },
        EmergencyContacts = [new EmergencyContactRequest { Relationship = Relationship.Spouse, Name = "Jane Doe", Phone = "9876500000" }],
    };

    // Under 18 must be NA; 18-or-older must be a real answer (Married/Unmarried), not NA.
    [Theory]
    [InlineData(MaritalStatus.Married, 0, false)]
    [InlineData(MaritalStatus.Married, 17, false)]
    [InlineData(MaritalStatus.Married, 18, true)]
    [InlineData(MaritalStatus.Married, 30, true)]
    [InlineData(MaritalStatus.Unmarried, 0, false)]
    [InlineData(MaritalStatus.Unmarried, 17, false)]
    [InlineData(MaritalStatus.Unmarried, 18, true)]
    [InlineData(MaritalStatus.NA, 0, true)]
    [InlineData(MaritalStatus.NA, 17, true)]
    [InlineData(MaritalStatus.NA, 18, false)]
    [InlineData(MaritalStatus.NA, 30, false)]
    public void IsMaritalStatusConsistentWithAge_MatchesExpectedBucket(MaritalStatus maritalStatus, int age, bool expected)
    {
        CreatePatientRequestValidator.IsMaritalStatusConsistentWithAge(maritalStatus, AgeYears(age)).Should().Be(expected);
    }

    [Fact]
    public void CreateValidator_RejectsNewbornRegisteredAsMarried()
    {
        var validator = new CreatePatientRequestValidator();
        // Baby is the only title consistent with a newborn's age — isolates this test to the
        // MaritalStatus rule rather than also tripping the Title/age check.
        var request = ValidCreateRequest(MaritalStatus.Married, DateOnly.FromDateTime(DateTime.UtcNow), Title.Baby);

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("MaritalStatus");
    }

    [Fact]
    public void CreateValidator_RejectsNewbornRegisteredAsUnmarried()
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(MaritalStatus.Unmarried, DateOnly.FromDateTime(DateTime.UtcNow), Title.Baby);

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("MaritalStatus");
    }

    [Fact]
    public void CreateValidator_RejectsAdultRegisteredAsNA()
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(MaritalStatus.NA, AgeYears(30));

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("MaritalStatus");
    }

    [Fact]
    public void CreateValidator_AcceptsAdultRegisteredAsMarried()
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(MaritalStatus.Married, AgeYears(30));

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor("MaritalStatus");
    }

    [Fact]
    public void CreateValidator_AcceptsAdultRegisteredAsUnmarried()
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(MaritalStatus.Unmarried, AgeYears(30));

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor("MaritalStatus");
    }

    [Fact]
    public void CreateValidator_AcceptsNewbornRegisteredAsNA()
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(MaritalStatus.NA, DateOnly.FromDateTime(DateTime.UtcNow), Title.Baby);

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor("MaritalStatus");
    }
}
