using FluentAssertions;
using HMS.Modules.ActivityLog.Application;
using HMS.Modules.ActivityLog.Contracts;
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
    private readonly IPatientIdentifierGenerator _identifierGenerator = Substitute.For<IPatientIdentifierGenerator>();
    private readonly IDepartmentService _departmentService = Substitute.For<IDepartmentService>();
    private readonly IConsultantService _consultantService = Substitute.For<IConsultantService>();
    private readonly IAppointmentTypeService _appointmentTypeService = Substitute.For<IAppointmentTypeService>();
    private readonly IConsultationTypeService _consultationTypeService = Substitute.For<IConsultationTypeService>();
    private readonly IActivityLogService _activityLog = Substitute.For<IActivityLogService>();
    private readonly PatientVisitService _sut;

    public PatientVisitServiceTests()
    {
        _sut = new PatientVisitService(
            _repository, _patientRepository, _identifierGenerator, _departmentService, _consultantService, _appointmentTypeService,
            _consultationTypeService, _activityLog, NullLogger<PatientVisitService>.Instance);

        // Happy-path defaults: the patient and every referenced Masters id exist.
        // Failure-path tests override these per-test.
        _patientRepository.ExistsAsync(PatientId, Arg.Any<CancellationToken>()).Returns(true);
        _identifierGenerator.NextVisitNumberAsync(VisitType.OP, Arg.Any<CancellationToken>()).Returns("OP-2026-000001");
        _identifierGenerator.NextVisitNumberAsync(Arg.Is<VisitType>(t => t != VisitType.OP), Arg.Any<CancellationToken>()).Returns("IP-2026-000001");
        _departmentService.GetByIdAsync(DepartmentId, Arg.Any<CancellationToken>())
            .Returns(Result<DepartmentResponse>.Success(new DepartmentResponse { Id = DepartmentId }));
        _consultantService.GetByIdAsync(ConsultantId, Arg.Any<CancellationToken>())
            .Returns(Result<ConsultantResponse>.Success(new ConsultantResponse
            {
                Id = ConsultantId,
                Name = "Dr. Karthikeyan",
                ConsultationTypeCharges = [new ConsultationTypeChargeDto(ConsultationTypeId, null)],
            }));
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
    public async Task CreateAsync_WithAConsultationTypeTheConsultantDoesNotOffer_FailsAndSavesNothing()
    {
        var otherTypeId = Guid.NewGuid();
        _consultationTypeService.GetByIdAsync(otherTypeId, Arg.Any<CancellationToken>())
            .Returns(Result<ConsultationTypeResponse>.Success(new ConsultationTypeResponse { Id = otherTypeId, Name = "Emergency / Casualty" }));

        var result = await _sut.CreateAsync(
            PatientId,
            NewRequest(new VisitConsultationRequest { DepartmentId = DepartmentId, ConsultantId = ConsultantId, ConsultationTypeId = otherTypeId }),
            actorId: null,
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.ConsultationTypeNotOffered);
        await _repository.DidNotReceive().AddAsync(Arg.Any<PatientVisit>(), Arg.Any<CancellationToken>());
    }

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

    // ADR-103: an OP visit gets its OP No only once its consultation fee is paid.
    [Fact]
    public async Task CreateAsync_ForAnOpVisit_DoesNotDrawAnOpNumber()
    {
        var result = await _sut.CreateAsync(PatientId, NewRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.VisitNumber.Should().BeNull();
        result.Value.VisitNumberPending.Should().BeTrue();
        await _identifierGenerator.DidNotReceive().NextVisitNumberAsync(Arg.Any<VisitType>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Has the repository's LockAndUpdateAsync run its update on <paramref name="visit"/>,
    /// the way the real one does once it holds the row lock.</summary>
    private void GivenLockedVisit(PatientVisit visit) =>
        _repository.LockAndUpdateAsync(visit.Id, Arg.Any<Func<PatientVisit, Task>>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await call.Arg<Func<PatientVisit, Task>>()(visit);
                return (PatientVisit?)visit;
            });

    [Fact]
    public async Task AssignOpNumberAsync_OnAnUnnumberedOpVisit_AssignsTheNextOpNumber()
    {
        var visit = PatientVisit.Create(PatientId, VisitType.OP, appointmentTypeId: null, createdBy: null);
        GivenLockedVisit(visit);

        var result = await _sut.AssignOpNumberAsync(PatientId, visit.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.VisitNumber.Should().Be("OP-2026-000001");
        visit.VisitNumber.Should().Be("OP-2026-000001");
    }

    [Fact]
    public async Task AssignOpNumberAsync_OnAVisitThatAlreadyHasOne_KeepsItAndDrawsNoNumber()
    {
        var visit = PatientVisit.Create(PatientId, VisitType.OP, appointmentTypeId: null, createdBy: null, visitNumber: "OP-2026-000007");
        GivenLockedVisit(visit);

        var result = await _sut.AssignOpNumberAsync(PatientId, visit.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.VisitNumber.Should().Be("OP-2026-000007");
        await _identifierGenerator.DidNotReceive().NextVisitNumberAsync(Arg.Any<VisitType>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignOpNumberAsync_OnANonOpVisit_ChangesNothing()
    {
        var visit = PatientVisit.Create(PatientId, VisitType.Emergency, appointmentTypeId: null, createdBy: null, visitNumber: "IP-2026-000003");
        GivenLockedVisit(visit);

        var result = await _sut.AssignOpNumberAsync(PatientId, visit.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.VisitNumber.Should().Be("IP-2026-000003");
        await _identifierGenerator.DidNotReceive().NextVisitNumberAsync(Arg.Any<VisitType>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignOpNumberAsync_ForAnotherPatientsVisit_FailsWithoutDrawingANumber()
    {
        var visit = PatientVisit.Create(Guid.NewGuid(), VisitType.OP, appointmentTypeId: null, createdBy: null);
        GivenLockedVisit(visit);

        var result = await _sut.AssignOpNumberAsync(PatientId, visit.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.VisitNotFound);
        visit.VisitNumber.Should().BeNull();
        await _identifierGenerator.DidNotReceive().NextVisitNumberAsync(Arg.Any<VisitType>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignOpNumberAsync_WhenTheVisitDoesNotExist_ReturnsVisitNotFound()
    {
        _repository.LockAndUpdateAsync(Arg.Any<Guid>(), Arg.Any<Func<PatientVisit, Task>>(), Arg.Any<CancellationToken>())
            .Returns((PatientVisit?)null);

        var result = await _sut.AssignOpNumberAsync(PatientId, Guid.NewGuid(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.VisitNotFound);
    }

    private static CreatePatientVisitRequest NonOpRequest(VisitType visitType) => NewRequest() with
    {
        VisitType = visitType,
        AppointmentTypeId = null,
        AdmissionType = MedicoLegalType.MLC,
        ArNumber = "AR/2026/1024",
        IsReferred = true,
        ReferredBy = ReferralSource.Doctor,
        Category = VisitCategory.B,
    };

    // ADR-104: an IP/Day Care/Observation visit gets its IP No once the advance on its
    // admission is paid, so none is drawn as it is recorded.
    [Theory]
    [InlineData(VisitType.IP)]
    [InlineData(VisitType.DayCare)]
    [InlineData(VisitType.Observation)]
    public async Task CreateAsync_ForAnIpDayCareOrObservationVisit_DrawsNoIpNumberAndSavesAdmissionDetails(VisitType visitType)
    {
        var result = await _sut.CreateAsync(PatientId, NonOpRequest(visitType), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.VisitNumber.Should().BeNull();
        result.Value.VisitNumberPending.Should().BeTrue();
        result.Value.AdmissionType.Should().Be(MedicoLegalType.MLC);
        result.Value.ArNumber.Should().Be("AR/2026/1024");
        result.Value.IsReferred.Should().BeTrue();
        result.Value.ReferredBy.Should().Be(ReferralSource.Doctor);
        result.Value.Category.Should().Be(VisitCategory.B);
        await _identifierGenerator.DidNotReceive().NextVisitNumberAsync(Arg.Any<VisitType>(), Arg.Any<CancellationToken>());
    }

    // Emergency care and MLC paperwork can't wait for a payment.
    [Fact]
    public async Task CreateAsync_ForAnEmergencyVisit_AssignsTheGeneratedIpNumberAtOnce()
    {
        var result = await _sut.CreateAsync(PatientId, NonOpRequest(VisitType.Emergency), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.VisitNumber.Should().Be("IP-2026-000001");
        result.Value.VisitNumberPending.Should().BeFalse();
    }

    [Theory]
    [InlineData(VisitType.IP)]
    [InlineData(VisitType.DayCare)]
    [InlineData(VisitType.Observation)]
    public async Task AssignIpNumberAsync_OnAnUnnumberedVisit_AssignsTheNextIpNumberAndLogsIt(VisitType visitType)
    {
        var visit = PatientVisit.Create(PatientId, visitType, appointmentTypeId: null, createdBy: null);
        GivenLockedVisit(visit);

        var result = await _sut.AssignIpNumberAsync(PatientId, visit.Id, IpNumberTrigger.AdvancePaid, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.VisitNumber.Should().Be("IP-2026-000001");
        result.Value.VisitNumberPending.Should().BeFalse();
        await _activityLog.Received(1).LogAsync(
            Arg.Is<ActivityLogRequest>(r => r.EntityId == visit.Id.ToString() && r.Description.Contains("IP-2026-000001") && r.Description.Contains("advance")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignIpNumberAsync_IssuedWithoutAdvance_SaysSoInTheActivityLog()
    {
        var visit = PatientVisit.Create(PatientId, VisitType.IP, appointmentTypeId: null, createdBy: null);
        GivenLockedVisit(visit);

        await _sut.AssignIpNumberAsync(PatientId, visit.Id, IpNumberTrigger.IssuedWithoutAdvance, actorId: null, CancellationToken.None);

        await _activityLog.Received(1).LogAsync(Arg.Is<ActivityLogRequest>(r => r.Description.Contains("without an advance")), Arg.Any<CancellationToken>());
    }

    // A paid advance never numbers an OP visit, and a paid consultation never numbers an IP one.
    [Fact]
    public async Task AssignIpNumberAsync_OnAnOpVisit_ChangesNothing()
    {
        var visit = PatientVisit.Create(PatientId, VisitType.OP, appointmentTypeId: null, createdBy: null);
        GivenLockedVisit(visit);

        var result = await _sut.AssignIpNumberAsync(PatientId, visit.Id, IpNumberTrigger.AdvancePaid, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.VisitNumber.Should().BeNull();
        await _identifierGenerator.DidNotReceive().NextVisitNumberAsync(Arg.Any<VisitType>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignOpNumberAsync_OnAnUnnumberedIpVisit_ChangesNothing()
    {
        var visit = PatientVisit.Create(PatientId, VisitType.IP, appointmentTypeId: null, createdBy: null);
        GivenLockedVisit(visit);

        var result = await _sut.AssignOpNumberAsync(PatientId, visit.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.VisitNumber.Should().BeNull();
        await _identifierGenerator.DidNotReceive().NextVisitNumberAsync(Arg.Any<VisitType>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignIpNumberAsync_OnAnEmergencyVisitThatAlreadyHasOne_KeepsIt()
    {
        var visit = PatientVisit.Create(PatientId, VisitType.Emergency, appointmentTypeId: null, createdBy: null, visitNumber: "IP-2026-000003");
        GivenLockedVisit(visit);

        var result = await _sut.AssignIpNumberAsync(PatientId, visit.Id, IpNumberTrigger.AdvancePaid, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.VisitNumber.Should().Be("IP-2026-000003");
        await _identifierGenerator.DidNotReceive().NextVisitNumberAsync(Arg.Any<VisitType>(), Arg.Any<CancellationToken>());
        await _activityLog.DidNotReceive().LogAsync(Arg.Any<ActivityLogRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenTheRequestIsRejected_DoesNotDrawAVisitNumber()
    {
        _patientRepository.ExistsAsync(PatientId, Arg.Any<CancellationToken>()).Returns(false);

        // An Emergency visit, the only kind that draws one at creation (ADR-103/ADR-104).
        await _sut.CreateAsync(PatientId, NonOpRequest(VisitType.Emergency), actorId: null, CancellationToken.None);

        await _identifierGenerator.DidNotReceive().NextVisitNumberAsync(Arg.Any<VisitType>(), Arg.Any<CancellationToken>());
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

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResultMappedFromRepository()
    {
        var visit = PatientVisit.Create(PatientId, VisitType.OP, AppointmentTypeId, createdBy: null);
        visit.AddConsultation(PatientVisitConsultation.Create(visit.Id, DepartmentId, ConsultantId, ConsultationTypeId), updatedBy: null);
        var query = new PatientVisitListQuery { Page = 1, PageSize = 20 };
        _repository.GetPagedAsync(query, Arg.Any<CancellationToken>())
            .Returns((new List<PatientVisit> { visit }, 1));

        var result = await _sut.GetAllAsync(query, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(v => v.PatientId == PatientId && v.Consultations.Count == 1);
    }
}
