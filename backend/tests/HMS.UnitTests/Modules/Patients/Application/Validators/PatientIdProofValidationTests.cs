using FluentValidation.TestHelper;
using HMS.Modules.Patients.Application.Validators;
using HMS.Modules.Patients.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Application.Validators;

public class PatientIdProofValidationTests
{
    private static readonly Guid StateId = Guid.NewGuid();
    private static readonly Guid DistrictId = Guid.NewGuid();

    private static CreatePatientRequest ValidCreateRequest(IdProofType? idProofType, string? idProofNumber) => new()
    {
        Title = Title.Mr,
        FirstName = "John",
        LastName = "Doe",
        DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-30),
        Gender = Gender.Male,
        BloodGroup = BloodGroup.Unknown,
        MaritalStatus = MaritalStatus.Married,
        PrimaryPhone = "9876543210",
        IdProofType = idProofType,
        IdProofNumber = idProofNumber,
        ModeOfArrivalSource = ModeOfArrivalSource.DoctorReferral,
        ModeOfArrivalChannel = "General Medicine",
        Address = new AddressRequest { AddressLine1 = "123 Main St", StateId = StateId, DistrictId = DistrictId, Pincode = "560001" },
        EmergencyContacts = [new EmergencyContactRequest { Relationship = Relationship.Spouse, Name = "Jane Doe", Phone = "9876500000" }],
    };

    [Theory]
    [InlineData(IdProofType.Passport, "A1234567", true)]
    [InlineData(IdProofType.Passport, "AB123456", false)] // two letters, not one
    [InlineData(IdProofType.Passport, "A123456", false)] // only 6 digits
    [InlineData(IdProofType.Passport, "uiop", false)] // the exact bad value that was slipping through
    [InlineData(IdProofType.VoterId, "ABC1234567", true)]
    [InlineData(IdProofType.VoterId, "AB1234567", false)] // two letters, not three
    [InlineData(IdProofType.VoterId, "uiop", false)]
    [InlineData(IdProofType.DrivingLicense, "KA0120210012345", true)] // real-shaped DL number
    [InlineData(IdProofType.DrivingLicense, "uiop", false)] // far too short to be a real DL number
    public void CreateValidator_ChecksFormatPerIdProofType(IdProofType idProofType, string idProofNumber, bool expectedValid)
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(idProofType, idProofNumber);

        var result = validator.TestValidate(request);

        if (expectedValid)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.IdProofNumber);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.IdProofNumber);
        }
    }

    // "Other" is a free-text catch-all by definition — no format check beyond NotEmpty.
    [Fact]
    public void CreateValidator_AcceptsAnyNonEmptyValueForOtherIdProofType()
    {
        var validator = new CreatePatientRequestValidator();
        var request = ValidCreateRequest(IdProofType.Other, "uiop");

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.IdProofNumber);
    }
}
