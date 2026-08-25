using FluentValidation.TestHelper;
using HMS.Modules.Patients.Application.Validators;
using HMS.Modules.Patients.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Application.Validators;

public class PatientBloodGroupValidationTests
{
    private static readonly Guid StateId = Guid.NewGuid();
    private static readonly Guid DistrictId = Guid.NewGuid();

    private static CreatePatientRequest ValidCreateRequest(BloodGroup bloodGroup) => new()
    {
        Title = Title.Mr,
        FirstName = "John",
        LastName = "Doe",
        DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-30),
        Gender = Gender.Male,
        BloodGroup = bloodGroup,
        MaritalStatus = MaritalStatus.Married,
        PrimaryPhone = "9876543210",
        ModeOfArrivalSource = ModeOfArrivalSource.DoctorReferral,
        Address = new AddressRequest { AddressLine1 = "123 Main St", StateId = StateId, DistrictId = DistrictId, Pincode = "560001" },
        EmergencyContacts = [new EmergencyContactRequest { Relationship = Relationship.Spouse, Name = "Jane Doe", Phone = "9876500000" }],
    };

    // BloodGroup is a required (non-nullable) enum now — "select Unknown if it isn't known"
    // is enforced by there being no way to submit the field empty at all, not by a separate
    // "missing" validation rule the way the old nullable field needed.
    [Fact]
    public void CreateValidator_AcceptsUnknownAsAnExplicitBloodGroupChoice()
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(BloodGroup.Unknown);

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.BloodGroup);
    }

    [Fact]
    public void CreateValidator_AcceptsAKnownBloodGroup()
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(BloodGroup.OPositive);

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.BloodGroup);
    }
}
