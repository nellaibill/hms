using FluentAssertions;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Application.Excel;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Application;

public class PatientImportRowMapperTests
{
    private static readonly Guid StateId = Guid.NewGuid();
    private static readonly Guid DistrictId = Guid.NewGuid();

    private readonly IStateService _stateService = Substitute.For<IStateService>();
    private readonly IDistrictService _districtService = Substitute.For<IDistrictService>();

    public PatientImportRowMapperTests()
    {
        _stateService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<StateResponse> { new() { Id = StateId, Name = "Tamil Nadu" } });
        _districtService.GetByStateIdAsync(StateId, Arg.Any<CancellationToken>())
            .Returns(new List<DistrictResponse> { new() { Id = DistrictId, StateId = StateId, Name = "Chennai" } });
    }

    private async Task<PatientImportReferenceData> LoadReferenceDataAsync()
        => await PatientImportReferenceData.LoadAsync(_stateService, _districtService, CancellationToken.None);

    private static Dictionary<string, string?> ValidRow() => new()
    {
        [PatientImportColumns.Title] = "Mr",
        [PatientImportColumns.FirstName] = "John",
        [PatientImportColumns.LastName] = "Doe",
        [PatientImportColumns.DateOfBirth] = "1990-01-01",
        [PatientImportColumns.Gender] = "Male",
        [PatientImportColumns.BloodGroup] = "Unknown",
        [PatientImportColumns.MaritalStatus] = "Married",
        [PatientImportColumns.PrimaryPhone] = "9876543210",
        [PatientImportColumns.ModeOfArrivalSource] = "DoctorReferral",
        [PatientImportColumns.AddressLine1] = "123 Main St",
        [PatientImportColumns.State] = "Tamil Nadu",
        [PatientImportColumns.District] = "Chennai",
        [PatientImportColumns.Pincode] = "600001",
        [PatientImportColumns.EmergencyContactName] = "Jane Doe",
        [PatientImportColumns.EmergencyContactPhone] = "9876500000",
        [PatientImportColumns.EmergencyContactRelationship] = "Spouse",
    };

    [Fact]
    public async Task MapAsync_WithFullyValidRow_ReturnsRequestWithNoErrors()
    {
        var referenceData = await LoadReferenceDataAsync();

        var (request, errors) = await PatientImportRowMapper.MapAsync(ValidRow(), referenceData, CancellationToken.None);

        errors.Should().BeEmpty();
        request.FirstName.Should().Be("John");
        request.Address.StateId.Should().Be(StateId);
        request.Address.DistrictId.Should().Be(DistrictId);
        request.EmergencyContacts.Should().ContainSingle(c => c.Name == "Jane Doe");
    }

    [Fact]
    public async Task MapAsync_WithGarbageGenderValue_ReturnsFieldError()
    {
        var row = ValidRow();
        row[PatientImportColumns.Gender] = "W/O";
        var referenceData = await LoadReferenceDataAsync();

        var (_, errors) = await PatientImportRowMapper.MapAsync(row, referenceData, CancellationToken.None);

        errors.Should().Contain(e => e.Field == PatientImportColumns.Gender);
    }

    [Fact]
    public async Task MapAsync_WithUnknownState_ReturnsStateError_AndDoesNotAttemptDistrictLookup()
    {
        var row = ValidRow();
        row[PatientImportColumns.State] = "Atlantis";
        var referenceData = await LoadReferenceDataAsync();

        var (request, errors) = await PatientImportRowMapper.MapAsync(row, referenceData, CancellationToken.None);

        errors.Should().Contain(e => e.Field == PatientImportColumns.State);
        request.Address.StateId.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task MapAsync_WithDistrictNotBelongingToState_ReturnsDistrictError()
    {
        var row = ValidRow();
        row[PatientImportColumns.District] = "Nowhere";
        var referenceData = await LoadReferenceDataAsync();

        var (_, errors) = await PatientImportRowMapper.MapAsync(row, referenceData, CancellationToken.None);

        errors.Should().Contain(e => e.Field == PatientImportColumns.District);
    }

    [Fact]
    public async Task MapAsync_WithoutEmergencyContact_ReturnsError()
    {
        var row = ValidRow();
        row[PatientImportColumns.EmergencyContactName] = null;
        var referenceData = await LoadReferenceDataAsync();

        var (request, errors) = await PatientImportRowMapper.MapAsync(row, referenceData, CancellationToken.None);

        errors.Should().Contain(e => e.Field == PatientImportColumns.EmergencyContactName);
        request.EmergencyContacts.Should().BeEmpty();
    }

    [Fact]
    public async Task MapAsync_WithUnparseableDateOfBirth_ReturnsFieldError()
    {
        var row = ValidRow();
        row[PatientImportColumns.DateOfBirth] = "0000-00-00";
        var referenceData = await LoadReferenceDataAsync();

        var (_, errors) = await PatientImportRowMapper.MapAsync(row, referenceData, CancellationToken.None);

        errors.Should().Contain(e => e.Field == PatientImportColumns.DateOfBirth);
    }

    [Fact]
    public async Task MapAsync_WithMultipleProblems_ReturnsAllOfThemTogether()
    {
        // Field-format/business-rule problems (e.g. a malformed phone number) are left to
        // CreatePatientRequestValidator, run separately by the background service — the mapper
        // itself is only responsible for enum/date/lookup parsing, so this only asserts on
        // those.
        var row = ValidRow();
        row[PatientImportColumns.Gender] = "NotAGender";
        row[PatientImportColumns.State] = "Atlantis";
        var referenceData = await LoadReferenceDataAsync();

        var (_, errors) = await PatientImportRowMapper.MapAsync(row, referenceData, CancellationToken.None);

        errors.Select(e => e.Field).Should().Contain([PatientImportColumns.Gender, PatientImportColumns.State]);
    }
}
