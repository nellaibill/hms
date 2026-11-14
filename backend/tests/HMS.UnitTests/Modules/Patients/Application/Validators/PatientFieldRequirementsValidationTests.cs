using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Application.Validators;
using HMS.Modules.Patients.Contracts;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Application.Validators;

/// <summary>Settings → Patient Registration Fields (ADR-135): the API follows the hospital's
/// mandatory-field choices, the bulk import doesn't.</summary>
public class PatientFieldRequirementsValidationTests
{
    private static readonly PatientFieldRequirements AllOptional = new()
    {
        LastName = false,
        BloodGroup = false,
        MaritalStatus = false,
        AddressLine1 = false,
        State = false,
        District = false,
        Pincode = false,
        EmergencyContact = false,
        ModeOfArrivalDetail = false,
        IdProofNumber = false,
    };

    private static CreatePatientRequest FullRequest() => new()
    {
        Title = Title.Mr,
        FirstName = "John",
        LastName = "Doe",
        DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-30),
        Gender = Gender.Male,
        BloodGroup = BloodGroup.Unknown,
        MaritalStatus = MaritalStatus.Married,
        PrimaryPhone = "9876543210",
        IdProofType = IdProofType.Aadhaar,
        IdProofNumber = "123412341234",
        ModeOfArrivalSource = ModeOfArrivalSource.DoctorReferral,
        ModeOfArrivalChannel = "General Medicine",
        Address = new AddressRequest { AddressLine1 = "123 Main St", StateId = Guid.NewGuid(), DistrictId = Guid.NewGuid(), Pincode = "560001" },
        EmergencyContacts = [new EmergencyContactRequest { Relationship = Relationship.Spouse, Name = "Jane Doe", Phone = "9876500000" }],
    };

    /// <summary>Everything a hospital can make optional, left blank.</summary>
    private static CreatePatientRequest BareRequest() => FullRequest() with
    {
        LastName = string.Empty,
        MaritalStatus = MaritalStatus.NA,
        IdProofType = null,
        IdProofNumber = null,
        ModeOfArrivalChannel = null,
        Address = new AddressRequest(),
        EmergencyContacts = [],
    };

    private static CreatePatientRequestValidator CreateValidator(PatientFieldRequirements requirements)
    {
        var settings = Substitute.For<IPatientFieldSettingsService>();
        settings.GetAsync(Arg.Any<CancellationToken>()).Returns(requirements);
        return new CreatePatientRequestValidator(settings);
    }

    private static UpdatePatientRequestValidator UpdateValidator(PatientFieldRequirements requirements)
    {
        var settings = Substitute.For<IPatientFieldSettingsService>();
        settings.GetAsync(Arg.Any<CancellationToken>()).Returns(requirements);
        return new UpdatePatientRequestValidator(settings);
    }

    private static IEnumerable<string> FailedProperties(ValidationResult result) => result.Errors.Select(e => e.PropertyName);

    [Fact]
    public async Task Create_WithTheDefaults_RejectsEverySwitchableFieldLeftBlank()
    {
        var result = await CreateValidator(PatientFieldRequirements.Default).ValidateAsync(BareRequest());

        FailedProperties(result).Should().Contain(
        [
            "LastName",
            "MaritalStatus",
            "IdProofNumber",
            "ModeOfArrivalChannel",
            "Address.AddressLine1",
            "Address.StateId",
            "Address.DistrictId",
            "Address.Pincode",
            "EmergencyContacts",
        ]);
    }

    [Fact]
    public async Task Create_WhenTheHospitalMakesThemOptional_AcceptsEverySwitchableFieldLeftBlank()
    {
        var result = await CreateValidator(AllOptional).ValidateAsync(BareRequest());

        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_WhenOptional_StillChecksTheFormatOfAnythingEntered()
    {
        var request = BareRequest() with
        {
            LastName = "D0e",
            Address = new AddressRequest { Pincode = "12" },
            IdProofType = IdProofType.Aadhaar,
            IdProofNumber = "123",
            EmergencyContacts = [new EmergencyContactRequest { Relationship = Relationship.Spouse, Name = "Jane", Phone = "" }],
        };

        var result = await CreateValidator(AllOptional).ValidateAsync(request);

        FailedProperties(result).Should().Contain(["LastName", "Address.Pincode", "IdProofNumber", "EmergencyContacts[0].Phone"]);
    }

    [Fact]
    public async Task Create_WithADistrictButNoState_IsRejectedEvenWhenStateIsOptional()
    {
        var request = BareRequest() with { Address = new AddressRequest { DistrictId = Guid.NewGuid() } };

        var result = await CreateValidator(AllOptional).ValidateAsync(request);

        FailedProperties(result).Should().Contain("Address.StateId");
    }

    [Fact]
    public async Task Create_WhenIdProofIsOptional_StillNeedsTheNumberOnceATypeIsSent()
    {
        var request = BareRequest() with { IdProofType = IdProofType.Passport, IdProofNumber = null };

        var result = await CreateValidator(AllOptional).ValidateAsync(request);

        FailedProperties(result).Should().ContainSingle().Which.Should().Be("IdProofNumber");
    }

    [Fact]
    public async Task Create_WhenMaritalStatusIsOptional_AMinorMustStillBeNotApplicable()
    {
        var request = BareRequest() with
        {
            Title = Title.Master,
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-10),
            MaritalStatus = MaritalStatus.Married,
        };

        var result = await CreateValidator(AllOptional).ValidateAsync(request);

        FailedProperties(result).Should().Contain("MaritalStatus");
    }

    [Fact]
    public async Task Create_ForTheBulkImport_IgnoresTheHospitalSettings()
    {
        var validator = CreateValidator(AllOptional);

        var result = await validator.ValidateAsync(PatientFieldRequirementsContext.ForBulkImport(BareRequest()));

        // The import template's own rules: everything mandatory except the ID proof number.
        FailedProperties(result).Should().Contain(["LastName", "Address.Pincode", "EmergencyContacts"]);
        FailedProperties(result).Should().NotContain("IdProofNumber");
    }

    [Fact]
    public void Create_ValidatedWithoutSettings_KeepsThePreviousApiRules()
    {
        var result = new CreatePatientRequestValidator().Validate(BareRequest());

        FailedProperties(result).Should().Contain(["LastName", "Address.StateId", "EmergencyContacts"]);
        FailedProperties(result).Should().NotContain("IdProofNumber");
    }

    [Fact]
    public async Task Update_FollowsTheSameSettings()
    {
        var request = new UpdatePatientRequest
        {
            Title = Title.Mr,
            FirstName = "John",
            LastName = string.Empty,
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-30),
            Gender = Gender.Male,
            BloodGroup = BloodGroup.Unknown,
            MaritalStatus = MaritalStatus.NA,
            PrimaryPhone = "9876543210",
            ModeOfArrivalSource = ModeOfArrivalSource.OnlineAdvertisement,
            Address = new AddressRequest(),
            RowVersion = "1",
        };

        var strict = await UpdateValidator(PatientFieldRequirements.Default).ValidateAsync(request);
        var relaxed = await UpdateValidator(AllOptional).ValidateAsync(request);

        FailedProperties(strict).Should().Contain(["LastName", "MaritalStatus", "IdProofNumber", "ModeOfArrivalChannel", "Address.Pincode"]);
        relaxed.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, true)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)] // a mandatory district needs a mandatory state
    public void SettingsValidator_KeepsStateMandatoryWhileDistrictIs(bool state, bool district, bool expectedValid)
    {
        var request = new PatientFieldRequirements { State = state, District = district };

        new UpdatePatientFieldSettingsRequestValidator().Validate(request).IsValid.Should().Be(expectedValid);
    }
}
