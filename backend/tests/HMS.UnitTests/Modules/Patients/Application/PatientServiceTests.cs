using FluentAssertions;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Application.Abstractions;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Application;

public class PatientServiceTests
{
    private static readonly byte[] ValidJpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];
    private static readonly byte[] ValidPngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];
    private static readonly byte[] ValidPdfBytes = "%PDF-1.4 rest of the file"u8.ToArray();
    private static readonly byte[] NotAFileBytes = "this is plain text, not a real file"u8.ToArray();
    private static readonly Guid DepartmentId = Guid.NewGuid();
    private static readonly Guid ConsultantId = Guid.NewGuid();

    private readonly IPatientRepository _repository = Substitute.For<IPatientRepository>();
    private readonly IPatientIdentifierGenerator _identifierGenerator = Substitute.For<IPatientIdentifierGenerator>();
    private readonly IPatientFileStorage _fileStorage = Substitute.For<IPatientFileStorage>();
    private readonly IDepartmentService _departmentService = Substitute.For<IDepartmentService>();
    private readonly IConsultantService _consultantService = Substitute.For<IConsultantService>();
    private readonly PatientService _sut;

    public PatientServiceTests()
    {
        _sut = new PatientService(_repository, _identifierGenerator, _fileStorage, _departmentService, _consultantService, NullLogger<PatientService>.Instance);

        _identifierGenerator.NextUhidAsync(Arg.Any<CancellationToken>()).Returns("P-2026-000001");
        _identifierGenerator.NextRegistrationNumberAsync(Arg.Any<EncounterType>(), Arg.Any<CancellationToken>()).Returns("OP-2026-000001");

        // Happy-path defaults: the department/consultant exist. Failure-path tests override these per-test.
        _departmentService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<DepartmentResponse>.Success(new DepartmentResponse()));
        _consultantService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<ConsultantResponse>.Success(new ConsultantResponse()));
    }

    private static CreatePatientRequest NewCreateRequest() => new()
    {
        Title = Title.Mr,
        FirstName = "John",
        LastName = "Doe",
        DateOfBirth = new DateOnly(1990, 1, 1),
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
            DepartmentId = DepartmentId,
            ConsultantId = ConsultantId,
        },
    };

    private static Patient NewPersistedPatient() => Patient.Create(
        "P-2026-000001", Title.Mr, "John", "Doe", new DateOnly(1990, 1, 1), Gender.Male, null,
        "123 Main St", null, null, "Central", "State", "560001",
        "9876543210", null, null, null, null, null, null, null,
        "Spouse", "Jane Doe", "9876500000", false, null, null, null);

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesPatientWithFirstRegistrationAndReturnsSuccess()
    {
        var request = NewCreateRequest();

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Uhid.Should().Be("P-2026-000001");
        result.Value.CurrentRegistration.Should().NotBeNull();
        result.Value.CurrentRegistration!.RegistrationNumber.Should().Be("OP-2026-000001");
        await _repository.Received(1).AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenDepartmentDoesNotExist_ReturnsInvalidDepartmentFailureAndDoesNotCreate()
    {
        _departmentService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<DepartmentResponse>.Failure("MASTERS.NOT_FOUND", "not found"));
        var request = NewCreateRequest();

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.InvalidDepartment);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenConsultantDoesNotExist_ReturnsInvalidConsultantFailureAndDoesNotCreate()
    {
        _consultantService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<ConsultantResponse>.Failure("MASTERS.NOT_FOUND", "not found"));
        var request = NewCreateRequest();

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.InvalidConsultant);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenSamePhoneAndNameAlreadyRegistered_ReturnsDuplicateFailureAndDoesNotCreate()
    {
        var request = NewCreateRequest();
        var existing = NewPersistedPatient();
        _repository.FindDuplicateAsync(request.PrimaryPhone, request.FirstName, request.LastName, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.DuplicatePatient);
        result.Error.Should().Contain(existing.Uhid);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Patient?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdatePatientRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_UpdatesAndReturnsSuccess()
    {
        var patient = NewPersistedPatient();
        _repository.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);

        var request = new UpdatePatientRequest
        {
            Title = Title.Dr,
            FirstName = "Johnny",
            LastName = "Doe",
            DateOfBirth = new DateOnly(1990, 1, 1),
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
    public async Task UploadPhotoAsync_WhenPatientNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Patient?)null);
        using var content = new MemoryStream(ValidJpegBytes);

        var result = await _sut.UploadPhotoAsync(Guid.NewGuid(), content, "photo.jpg", "image/jpeg", content.Length, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.NotFound);
        await _fileStorage.DidNotReceive().SaveAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadPhotoAsync_WithValidJpeg_UploadsAndReturnsSuccess()
    {
        var patient = NewPersistedPatient();
        _repository.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);
        _fileStorage.SaveAsync(patient.Id, "photo", "photo.jpg", Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("uploads/patients/x/photo/y.jpg");
        using var content = new MemoryStream(ValidJpegBytes);

        var result = await _sut.UploadPhotoAsync(patient.Id, content, "photo.jpg", "image/jpeg", content.Length, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PhotoPath.Should().Be("uploads/patients/x/photo/y.jpg");
    }

    [Fact]
    public async Task UploadPhotoAsync_WithValidPng_UploadsAndReturnsSuccess()
    {
        var patient = NewPersistedPatient();
        _repository.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);
        _fileStorage.SaveAsync(patient.Id, "photo", "photo.png", Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("uploads/patients/x/photo/y.png");
        using var content = new MemoryStream(ValidPngBytes);

        var result = await _sut.UploadPhotoAsync(patient.Id, content, "photo.png", "image/png", content.Length, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UploadPhotoAsync_WithDisallowedContentType_ReturnsInvalidFileFailure()
    {
        var patient = NewPersistedPatient();
        _repository.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);
        using var content = new MemoryStream(ValidPdfBytes);

        var result = await _sut.UploadPhotoAsync(patient.Id, content, "id.pdf", "application/pdf", content.Length, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.InvalidFile);
        await _fileStorage.DidNotReceive().SaveAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadPhotoAsync_WithOversizedFile_ReturnsInvalidFileFailure()
    {
        var patient = NewPersistedPatient();
        _repository.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);
        using var content = new MemoryStream(ValidJpegBytes);
        const long oversizedLength = 6 * 1024 * 1024; // 6MB, over the 5MB limit.

        var result = await _sut.UploadPhotoAsync(patient.Id, content, "photo.jpg", "image/jpeg", oversizedLength, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.InvalidFile);
    }

    [Fact]
    public async Task UploadPhotoAsync_WithCorruptedImage_ReturnsInvalidFileFailure()
    {
        // "virus.jpg" — extension and declared Content-Type both claim JPEG, but the bytes
        // are plain text. Only the magic-byte check catches this.
        var patient = NewPersistedPatient();
        _repository.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);
        using var content = new MemoryStream(NotAFileBytes);

        var result = await _sut.UploadPhotoAsync(patient.Id, content, "virus.jpg", "image/jpeg", content.Length, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.InvalidFile);
        await _fileStorage.DidNotReceive().SaveAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadIdProofAsync_WhenPatientNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Patient?)null);
        using var content = new MemoryStream(ValidPdfBytes);

        var result = await _sut.UploadIdProofAsync(Guid.NewGuid(), IdProofType.Aadhaar, content, "id.pdf", "application/pdf", content.Length, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.NotFound);
    }

    [Fact]
    public async Task UploadIdProofAsync_WithValidPdf_UploadsAndReturnsSuccess()
    {
        var patient = NewPersistedPatient();
        _repository.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);
        _fileStorage.SaveAsync(patient.Id, "id-proof", "id.pdf", Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("uploads/patients/x/id-proof/y.pdf");
        using var content = new MemoryStream(ValidPdfBytes);

        var result = await _sut.UploadIdProofAsync(patient.Id, IdProofType.Aadhaar, content, "id.pdf", "application/pdf", content.Length, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IdProofType.Should().Be(IdProofType.Aadhaar);
        result.Value.IdProofPath.Should().Be("uploads/patients/x/id-proof/y.pdf");
    }

    [Fact]
    public async Task UploadIdProofAsync_WithCorruptedPdf_ReturnsInvalidFileFailure()
    {
        // A renamed non-PDF ("id.pdf" claiming application/pdf) is still rejected by the
        // "%PDF-" signature check — the same protection JPEG/PNG get.
        var patient = NewPersistedPatient();
        _repository.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);
        using var content = new MemoryStream(NotAFileBytes);

        var result = await _sut.UploadIdProofAsync(patient.Id, IdProofType.Aadhaar, content, "id.pdf", "application/pdf", content.Length, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.InvalidFile);
    }

    [Fact]
    public async Task AddRegistrationAsync_WhenPatientNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Patient?)null);

        var result = await _sut.AddRegistrationAsync(Guid.NewGuid(), new PatientRegistrationDetails(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.NotFound);
    }

    [Fact]
    public async Task AddRegistrationAsync_WhenPatientFound_AddsRegistrationAndReturnsSuccess()
    {
        var patient = NewPersistedPatient();
        _repository.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);
        _identifierGenerator.NextRegistrationNumberAsync(EncounterType.IP, Arg.Any<CancellationToken>()).Returns("IP-2026-000005");

        var request = new PatientRegistrationDetails
        {
            EncounterType = EncounterType.IP,
            ModeOfArrival = ModeOfArrival.Ambulance,
            DepartmentId = DepartmentId,
            ConsultantId = ConsultantId,
            AdmissionType = AdmissionType.MLC,
        };

        var result = await _sut.AddRegistrationAsync(patient.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RegistrationNumber.Should().Be("IP-2026-000005");
        result.Value.DepartmentId.Should().Be(DepartmentId);
        patient.Registrations.Should().ContainSingle();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        // Adding a follow-up registration is not itself an edit to the Patient master
        // record — Create's counterpart never calls AddAsync a second time either.
        await _repository.DidNotReceive().AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRegistrationsAsync_WhenPatientNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Patient?)null);

        var result = await _sut.GetRegistrationsAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetRegistrationsAsync_ReturnsAllRegistrationsNewestFirst()
    {
        var patient = NewPersistedPatient();
        var first = PatientRegistration.Create(patient.Id, "OP-2026-000001", EncounterType.OP, ModeOfArrival.WalkIn, DepartmentId, ConsultantId, null, null, null, null);
        patient.AddRegistration(first);
        Thread.Sleep(5); // Ensure a distinguishable CreatedAt for ordering.
        var second = PatientRegistration.Create(patient.Id, "IP-2026-000002", EncounterType.IP, ModeOfArrival.Ambulance, DepartmentId, ConsultantId, AdmissionType.NMLC, null, null, null);
        patient.AddRegistration(second);
        _repository.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);

        var result = await _sut.GetRegistrationsAsync(patient.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].RegistrationNumber.Should().Be("IP-2026-000002");
        result.Value[1].RegistrationNumber.Should().Be("OP-2026-000001");
    }
}
