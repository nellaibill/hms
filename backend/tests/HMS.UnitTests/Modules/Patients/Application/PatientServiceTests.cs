using FluentAssertions;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Application.Abstractions;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Application;

public class PatientServiceTests
{
    private static readonly Guid StateId = Guid.NewGuid();
    private static readonly Guid DistrictId = Guid.NewGuid();

    private readonly IPatientRepository _repository = Substitute.For<IPatientRepository>();
    private readonly IPatientIdentifierGenerator _identifierGenerator = Substitute.For<IPatientIdentifierGenerator>();
    private readonly IStateService _stateService = Substitute.For<IStateService>();
    private readonly IDistrictService _districtService = Substitute.For<IDistrictService>();
    private readonly PatientService _sut;

    public PatientServiceTests()
    {
        _sut = new PatientService(_repository, _identifierGenerator, _stateService, _districtService, NullLogger<PatientService>.Instance);

        _identifierGenerator.NextUhidAsync(Arg.Any<CancellationToken>()).Returns("P-2026-000001");

        // Happy-path defaults: the state/district exist and belong to each other.
        // Failure-path tests override these per-test.
        _stateService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<StateResponse> { new() { Id = StateId } });
        _districtService.GetByStateIdAsync(StateId, Arg.Any<CancellationToken>())
            .Returns(new List<DistrictResponse> { new() { Id = DistrictId } });
    }

    private static AddressRequest NewAddressRequest() => new()
    {
        AddressLine1 = "123 Main St",
        StateId = StateId,
        DistrictId = DistrictId,
        Pincode = "560001",
    };

    private static CreatePatientRequest NewCreateRequest() => new()
    {
        Title = Title.Mr,
        FirstName = "John",
        LastName = "Doe",
        DateOfBirth = new DateOnly(1990, 1, 1),
        Gender = Gender.Male,
        BloodGroup = BloodGroup.Unknown,
        MaritalStatus = MaritalStatus.Married,
        PrimaryPhone = "9876543210",
        ModeOfArrivalSource = ModeOfArrivalSource.DoctorReferral,
        Address = NewAddressRequest(),
        EmergencyContacts = [new EmergencyContactRequest { Relationship = Relationship.Spouse, Name = "Jane Doe", Phone = "9876500000" }],
    };

    private static Patient NewPersistedPatient()
    {
        var patient = Patient.Create(
            "P-2026-000001", Title.Mr, "John", "Doe", new DateOnly(1990, 1, 1), Gender.Male, BloodGroup.Unknown, MaritalStatus.Married,
            "9876543210", null, null, null, null, null, ModeOfArrivalSource.DoctorReferral, null, null, createdBy: null);
        patient.SetAddress(Address.Create(patient.Id, "123 Main St", null, null, StateId, DistrictId, "560001"));
        patient.AddEmergencyContact(EmergencyContact.Create(patient.Id, Relationship.Spouse, "Jane Doe", "9876500000"), updatedBy: null);
        return patient;
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesPatientAndReturnsSuccess()
    {
        var request = NewCreateRequest();

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Uhid.Should().Be("P-2026-000001");
        result.Value.EmergencyContacts.Should().ContainSingle(c => c.Name == "Jane Doe");
        await _repository.Received(1).AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithAllergiesSupplied_AddsThemToTheAggregate()
    {
        var request = NewCreateRequest() with
        {
            Allergies = [new AllergyRequest { AllergyType = AllergyType.Drug, Specify = "Penicillin", Severity = AllergySeverity.Severe }],
        };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Allergies.Should().ContainSingle(a => a.Specify == "Penicillin" && a.Severity == AllergySeverity.Severe);
    }

    [Fact]
    public async Task CreateAsync_WhenStateDoesNotExist_ReturnsInvalidStateFailureAndDoesNotCreate()
    {
        _stateService.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<StateResponse>());
        var request = NewCreateRequest();

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.InvalidState);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenDistrictDoesNotBelongToState_ReturnsInvalidDistrictFailureAndDoesNotCreate()
    {
        _districtService.GetByStateIdAsync(StateId, Arg.Any<CancellationToken>()).Returns(new List<DistrictResponse>());
        var request = NewCreateRequest();

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.InvalidDistrict);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenSamePhoneAndNameAlreadyRegistered_ReturnsDuplicateFailureAndDoesNotCreate()
    {
        var request = NewCreateRequest();
        var existing = NewPersistedPatient();
        _repository.FindDuplicateAsync(request.PrimaryPhone, request.FirstName, request.LastName, request.IdProofNumber, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.DuplicatePatient);
        result.Error.Should().Contain(existing.Uhid);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Patient?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdatePatientRequest { Address = NewAddressRequest() }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_WhenRowVersionDoesNotMatchLoadedPatient_ReturnsConcurrencyConflictAndDoesNotSave()
    {
        var patient = NewPersistedPatient();
        _repository.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);
        _repository.GetRowVersion(patient).Returns("42");

        var request = new UpdatePatientRequest
        {
            Title = Title.Dr,
            FirstName = "Johnny",
            LastName = "Doe",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = Gender.Male,
            BloodGroup = BloodGroup.Unknown,
            MaritalStatus = MaritalStatus.Married,
            PrimaryPhone = "9876543210",
            ModeOfArrivalSource = ModeOfArrivalSource.DoctorReferral,
            Address = NewAddressRequest(),
            // Stale — someone else's edit moved the row to version "42" after this client loaded "1".
            RowVersion = "1",
        };

        var result = await _sut.UpdateAsync(patient.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.ConcurrencyConflict);
        patient.FirstName.Should().Be("John", "a conflicting update must not be applied to the entity at all");
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenRowVersionMatchesLoadedPatient_UpdatesAndReturnsSuccess()
    {
        var patient = NewPersistedPatient();
        _repository.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);
        _repository.GetRowVersion(patient).Returns("42");

        var request = new UpdatePatientRequest
        {
            Title = Title.Dr,
            FirstName = "Johnny",
            LastName = "Doe",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = Gender.Male,
            BloodGroup = BloodGroup.Unknown,
            MaritalStatus = MaritalStatus.Married,
            PrimaryPhone = "9876543210",
            ModeOfArrivalSource = ModeOfArrivalSource.DoctorReferral,
            Address = NewAddressRequest(),
            RowVersion = "42",
        };

        var result = await _sut.UpdateAsync(patient.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.FirstName.Should().Be("Johnny");
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_SoftDeletesAndReturnsSuccess()
    {
        var patient = NewPersistedPatient();
        _repository.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);

        var result = await _sut.DeleteAsync(patient.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        patient.IsDeleted.Should().BeTrue();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Patient?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsSuccess()
    {
        var patient = NewPersistedPatient();
        _repository.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);

        var result = await _sut.GetByIdAsync(patient.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(patient.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Patient?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPagedResult()
    {
        var patient = NewPersistedPatient();
        _repository.GetPagedAsync(Arg.Any<PatientListQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<Patient> { patient }, 1));

        var result = await _sut.GetPagedAsync(new PatientListQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle(p => p.Id == patient.Id);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task AddAllergyAsync_WhenPatientFound_AddsAllergyAndReturnsSuccess()
    {
        var patient = NewPersistedPatient();
        _repository.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);
        var request = new AddAllergyRequest { AllergyType = AllergyType.Food, Specify = "Peanuts", Severity = AllergySeverity.Moderate };

        var result = await _sut.AddAllergyAsync(patient.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Allergies.Should().ContainSingle(a => a.Specify == "Peanuts");
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAllergyAsync_WhenPatientNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Patient?)null);

        var result = await _sut.AddAllergyAsync(Guid.NewGuid(), new AddAllergyRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.NotFound);
    }

    [Fact]
    public async Task RemoveAllergyAsync_WhenAllergyExists_RemovesItAndReturnsSuccess()
    {
        var patient = NewPersistedPatient();
        var allergy = Allergy.Create(patient.Id, AllergyType.Food, "Peanuts", AllergySeverity.Moderate);
        patient.AddAllergy(allergy, updatedBy: null);
        _repository.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);

        var result = await _sut.RemoveAllergyAsync(patient.Id, allergy.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Allergies.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveAllergyAsync_WhenAllergyDoesNotExist_ReturnsAllergyNotFoundFailure()
    {
        var patient = NewPersistedPatient();
        _repository.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);

        var result = await _sut.RemoveAllergyAsync(patient.Id, Guid.NewGuid(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.AllergyNotFound);
    }

    [Fact]
    public async Task AddEmergencyContactAsync_WhenPatientFound_AddsContactAndReturnsSuccess()
    {
        var patient = NewPersistedPatient();
        _repository.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);
        var request = new AddEmergencyContactRequest { Relationship = Relationship.Friend, Name = "Sam", Phone = "9123456780" };

        var result = await _sut.AddEmergencyContactAsync(patient.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EmergencyContacts.Should().Contain(c => c.Name == "Sam");
        result.Value.EmergencyContacts.Should().HaveCount(2);
    }

    [Fact]
    public async Task RemoveEmergencyContactAsync_WhenMoreThanOneRemains_RemovesItAndReturnsSuccess()
    {
        var patient = NewPersistedPatient();
        var second = EmergencyContact.Create(patient.Id, Relationship.Friend, "Sam", "9123456780");
        patient.AddEmergencyContact(second, updatedBy: null);
        _repository.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);

        var result = await _sut.RemoveEmergencyContactAsync(patient.Id, second.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EmergencyContacts.Should().ContainSingle();
    }

    [Fact]
    public async Task RemoveEmergencyContactAsync_WhenItIsTheOnlyOne_ReturnsCannotRemoveLastFailure()
    {
        var patient = NewPersistedPatient();
        var onlyContact = patient.EmergencyContacts.Single();
        _repository.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);

        var result = await _sut.RemoveEmergencyContactAsync(patient.Id, onlyContact.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.CannotRemoveLastEmergencyContact);
        patient.EmergencyContacts.Should().ContainSingle("the only emergency contact must not actually be removed from the entity");
    }

    [Fact]
    public async Task RemoveEmergencyContactAsync_WhenContactDoesNotExist_ReturnsEmergencyContactNotFoundFailure()
    {
        var patient = NewPersistedPatient();
        _repository.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);

        var result = await _sut.RemoveEmergencyContactAsync(patient.Id, Guid.NewGuid(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.EmergencyContactNotFound);
    }
}
