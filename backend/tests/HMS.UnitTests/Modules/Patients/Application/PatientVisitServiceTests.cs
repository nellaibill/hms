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

public class PatientVisitServiceTests
{
    private static readonly Guid PatientId = Guid.NewGuid();
    private static readonly Guid DepartmentId = Guid.NewGuid();
    private static readonly Guid ConsultantId = Guid.NewGuid();
    private static readonly Guid AppointmentTypeId = Guid.NewGuid();
    private static readonly Guid ConsultationTypeId = Guid.NewGuid();

    private readonly IPatientVisitRepository _repository = Substitute.For<IPatientVisitRepository>();
    private readonly IPatientRepository _patientRepository = Substitute.For<IPatientRepository>();
    private readonly IDepartmentService _departmentService = Substitute.For<IDepartmentService>();
    private readonly IConsultantService _consultantService = Substitute.For<IConsultantService>();
    private readonly IAppointmentTypeService _appointmentTypeService = Substitute.For<IAppointmentTypeService>();
    private readonly IConsultationTypeService _consultationTypeService = Substitute.For<IConsultationTypeService>();
    private readonly PatientVisitService _sut;

    public PatientVisitServiceTests()
    {
        _sut = new PatientVisitService(
            _repository, _patientRepository, _departmentService, _consultantService, _appointmentTypeService, _consultationTypeService,
            NullLogger<PatientVisitService>.Instance);

        // Happy-path defaults: the patient and every referenced Masters id exist.
        // Failure-path tests override these per-test.
        _patientRepository.ExistsAsync(PatientId, Arg.Any<CancellationToken>()).Returns(true);
        _departmentService.GetByIdAsync(DepartmentId, Arg.Any<CancellationToken>())
            .Returns(Result<DepartmentResponse>.Success(new DepartmentResponse { Id = DepartmentId }));
        _consultantService.GetByIdAsync(ConsultantId, Arg.Any<CancellationToken>())
            .Returns(Result<ConsultantResponse>.Success(new ConsultantResponse { Id = ConsultantId }));
        _appointmentTypeService.GetByIdAsync(AppointmentTypeId, Arg.Any<CancellationToken>())
            .Returns(Result<AppointmentTypeResponse>.Success(new AppointmentTypeResponse { Id = AppointmentTypeId }));
        _consultationTypeService.GetByIdAsync(ConsultationTypeId, Arg.Any<CancellationToken>())
            .Returns(Result<ConsultationTypeResponse>.Success(new ConsultationTypeResponse { Id = ConsultationTypeId }));
    }

    private static CreatePatientVisitRequest NewRequest(params VisitConsultationRequest[] consultations) => new()
    {
        VisitType = VisitType.OP,
        AppointmentTypeId = AppointmentTypeId,
        Consultations = consultations.Length > 0
            ? consultations
            : [new VisitConsultationRequest { DepartmentId = DepartmentId, ConsultantId = ConsultantId, ConsultationTypeId = ConsultationTypeId }],
    };

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesVisitAndReturnsSuccess()
    {
        var result = await _sut.CreateAsync(PatientId, NewRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PatientId.Should().Be(PatientId);
        result.Value.VisitType.Should().Be(VisitType.OP);
        result.Value.Consultations.Should().ContainSingle(c => c.DepartmentId == DepartmentId && c.ConsultantId == ConsultantId);
        await _repository.Received(1).AddAsync(Arg.Any<PatientVisit>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithFourConsultationLines_SharesOneVisitIdAcrossAll()
    {
        var request = NewRequest(
            new VisitConsultationRequest { DepartmentId = DepartmentId, ConsultantId = ConsultantId },
            new VisitConsultationRequest { DepartmentId = DepartmentId, ConsultantId = ConsultantId },
            new VisitConsultationRequest { DepartmentId = DepartmentId, ConsultantId = ConsultantId },
            new VisitConsultationRequest { DepartmentId = DepartmentId, ConsultantId = ConsultantId });

        PatientVisit? captured = null;
        await _repository.AddAsync(Arg.Do<PatientVisit>(v => captured = v), Arg.Any<CancellationToken>());

        var result = await _sut.CreateAsync(PatientId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Consultations.Should().HaveCount(4);
        captured.Should().NotBeNull();
        captured!.Consultations.Should().OnlyContain(c => c.VisitId == captured.Id);
    }

    [Fact]
    public async Task CreateAsync_WhenPatientDoesNotExist_ReturnsNotFound()
    {
        _patientRepository.ExistsAsync(PatientId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.CreateAsync(PatientId, NewRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.NotFound);
    }

    [Fact]
    public async Task CreateAsync_WhenDepartmentDoesNotExist_ReturnsInvalidDepartment()
    {
        _departmentService.GetByIdAsync(DepartmentId, Arg.Any<CancellationToken>())
            .Returns(Result<DepartmentResponse>.Failure("NOT_FOUND", "not found"));

        var result = await _sut.CreateAsync(PatientId, NewRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.InvalidDepartment);
    }

    [Fact]
    public async Task CreateAsync_WhenConsultantDoesNotExist_ReturnsInvalidConsultant()
    {
        _consultantService.GetByIdAsync(ConsultantId, Arg.Any<CancellationToken>())
            .Returns(Result<ConsultantResponse>.Failure("NOT_FOUND", "not found"));

        var result = await _sut.CreateAsync(PatientId, NewRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.InvalidConsultant);
    }

    [Fact]
    public async Task CreateAsync_WhenAppointmentTypeDoesNotExist_ReturnsInvalidAppointmentType()
    {
        _appointmentTypeService.GetByIdAsync(AppointmentTypeId, Arg.Any<CancellationToken>())
            .Returns(Result<AppointmentTypeResponse>.Failure("NOT_FOUND", "not found"));

        var result = await _sut.CreateAsync(PatientId, NewRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.InvalidAppointmentType);
    }

    [Fact]
    public async Task CreateAsync_WhenConsultationTypeDoesNotExist_ReturnsInvalidConsultationType()
    {
        _consultationTypeService.GetByIdAsync(ConsultationTypeId, Arg.Any<CancellationToken>())
            .Returns(Result<ConsultationTypeResponse>.Failure("NOT_FOUND", "not found"));

        var result = await _sut.CreateAsync(PatientId, NewRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.InvalidConsultationType);
    }

    [Fact]
    public async Task CreateAsync_WithoutAppointmentTypeOrConsultationType_Succeeds()
    {
        var request = new CreatePatientVisitRequest
        {
            VisitType = VisitType.Emergency,
            AppointmentTypeId = null,
            Consultations = [new VisitConsultationRequest { DepartmentId = DepartmentId, ConsultantId = ConsultantId, ConsultationTypeId = null }],
        };

        var result = await _sut.CreateAsync(PatientId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AppointmentTypeId.Should().BeNull();
    }
}
