using FluentAssertions;
using FluentValidation.TestHelper;
using HMS.Modules.Patients.Application.Validators;
using HMS.Modules.Patients.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Application.Validators;

public class PatientBloodGroupValidationTests
{
    private static CreatePatientRequest ValidCreateRequest(BloodGroup? bloodGroup) => new()
    {
        Title = Title.Mr,
        FirstName = "John",
        LastName = "Doe",
        DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-30),
        Gender = Gender.Male,
        BloodGroup = bloodGroup,
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

    [Fact]
    public void CreateValidator_RejectsMissingBloodGroup()
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(bloodGroup: null);

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.BloodGroup);
    }

    [Fact]
    public void CreateValidator_AcceptsUnknownAsAnExplicitBloodGroupChoice()
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(bloodGroup: BloodGroup.Unknown);

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.BloodGroup);
    }

    [Fact]
    public void CreateValidator_AcceptsAKnownBloodGroup()
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(bloodGroup: BloodGroup.OPositive);

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.BloodGroup);
    }

    [Fact]
    public void UpdateValidator_RejectsMissingBloodGroup()
    {
        var validator = new UpdatePatientRequestValidator();
        var request = new UpdatePatientRequest
        {
            Title = Title.Mr,
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-30),
            Gender = Gender.Male,
            BloodGroup = null,
            AddressLine1 = "123 Main St",
            District = "Central",
            State = "State",
            Pincode = "560001",
            PrimaryPhone = "9876543210",
            EmergencyContactRelationship = "Spouse",
            EmergencyContactName = "Jane Doe",
            EmergencyContactPhone = "9876500000",
            RowVersion = "1",
        };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.BloodGroup);
    }
}
