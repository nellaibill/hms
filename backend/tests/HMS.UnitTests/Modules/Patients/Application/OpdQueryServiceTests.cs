using FluentAssertions;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Application.Abstractions;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using HMS.Shared.Kernel;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Application;

public class OpdQueryServiceTests
{
    private static readonly Guid PatientId = Guid.NewGuid();
    private static readonly Guid VisitId = Guid.NewGuid();
    private static readonly Guid ConsultationId = Guid.NewGuid();
    private static readonly Guid DepartmentId = Guid.NewGuid();
    private static readonly Guid ConsultantId = Guid.NewGuid();

    private readonly IPatientVisitRepository _repository = Substitute.For<IPatientVisitRepository>();
    private readonly IDepartmentService _departmentService = Substitute.For<IDepartmentService>();
    private readonly IConsultantService _consultantService = Substitute.For<IConsultantService>();
    private readonly IAppointmentTypeService _appointmentTypeService = Substitute.For<IAppointmentTypeService>();
    private readonly OpdQueryService _sut;

    public OpdQueryServiceTests()
    {
        _sut = new OpdQueryService(_repository, _departmentService, _consultantService, _appointmentTypeService);

        _departmentService.GetByIdAsync(DepartmentId, Arg.Any<CancellationToken>())
            .Returns(Result<DepartmentResponse>.Success(new DepartmentResponse { Id = DepartmentId, Name = "Cardiology" }));
        _consultantService.GetByIdAsync(ConsultantId, Arg.Any<CancellationToken>())
            .Returns(Result<ConsultantResponse>.Success(new ConsultantResponse { Id = ConsultantId, Name = "Dr. Rao" }));
    }

    private static OpdPatientListRow NewRow(OpdConsultationStatus status = OpdConsultationStatus.Waiting) => new(
        ConsultationId,
        VisitId,
        PatientId,
        "UHID001",
        "9876543210",
        "John",
        "Doe",
        new DateOnly(1990, 1, 1),
        Gender.Male,
        new DateTime(2026, 9, 11, 9, 0, 0, DateTimeKind.Utc),
        null,
        DepartmentId,
        ConsultantId,
        status);

    [Fact]
    public async Task GetPatientListAsync_MapsRowsAndResolvesDisplayNames()
    {
        var query = new OpdPatientListQuery { Page = 1, PageSize = 20 };
        _repository.GetOpdPatientListPagedAsync(query, Arg.Any<CancellationToken>())
            .Returns((new List<OpdPatientListRow> { NewRow() }, 1));

        var result = await _sut.GetPatientListAsync(query, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        var item = result.Items.Should().ContainSingle().Subject;
        item.ConsultationId.Should().Be(ConsultationId);
        item.PatientName.Should().Be("John Doe");
        item.Uhid.Should().Be("UHID001");
        item.PhoneNumber.Should().Be("9876543210");
        item.DepartmentName.Should().Be("Cardiology");
        item.ConsultantName.Should().Be("Dr. Rao");
        item.Status.Should().Be(OpdConsultationStatus.Waiting);
        item.Age.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetConsultationSummaryAsync_GroupsByConsultantAndCountsEachStatus()
    {
        var rows = new List<OpdPatientListRow>
        {
            NewRow(OpdConsultationStatus.Waiting),
            NewRow(OpdConsultationStatus.InConsultation),
            NewRow(OpdConsultationStatus.Completed),
            NewRow(OpdConsultationStatus.Completed),
        };
        _repository.GetOpdConsultationSummaryRowsAsync(Arg.Any<OpdConsultationSummaryQuery>(), Arg.Any<CancellationToken>())
            .Returns(rows);

        var result = await _sut.GetConsultationSummaryAsync(new OpdConsultationSummaryQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var summary = result.Value.Should().ContainSingle().Subject;
        summary.ConsultantId.Should().Be(ConsultantId);
        summary.ConsultantName.Should().Be("Dr. Rao");
        summary.DepartmentName.Should().Be("Cardiology");
        summary.TotalPatients.Should().Be(4);
        summary.Waiting.Should().Be(1);
        summary.InConsultation.Should().Be(1);
        summary.Completed.Should().Be(2);
    }

    [Fact]
    public async Task TransitionAsync_WhenConsultationNotFound_ReturnsFailure()
    {
        _repository.GetByConsultationIdAsync(ConsultationId, Arg.Any<CancellationToken>()).Returns((PatientVisit?)null);

        var result = await _sut.TransitionAsync(ConsultationId, "check-in", actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.ConsultationNotFound);
    }

    [Fact]
    public async Task TransitionAsync_CheckIn_UpdatesStatusAndSaves()
    {
        var visit = PatientVisit.Create(PatientId, VisitType.OP, appointmentTypeId: null, createdBy: null);
        visit.AddConsultation(PatientVisitConsultation.Create(visit.Id, DepartmentId, ConsultantId, consultationTypeId: null), updatedBy: null);
        var consultation = visit.Consultations.Single();
        _repository.GetByConsultationIdAsync(consultation.Id, Arg.Any<CancellationToken>()).Returns(visit);

        var result = await _sut.TransitionAsync(consultation.Id, "check-in", actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(OpdConsultationStatus.CheckedIn);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransitionAsync_IllegalTransition_ReturnsFailureAndDoesNotSave()
    {
        var visit = PatientVisit.Create(PatientId, VisitType.OP, appointmentTypeId: null, createdBy: null);
        visit.AddConsultation(PatientVisitConsultation.Create(visit.Id, DepartmentId, ConsultantId, consultationTypeId: null), updatedBy: null);
        var consultation = visit.Consultations.Single();
        _repository.GetByConsultationIdAsync(consultation.Id, Arg.Any<CancellationToken>()).Returns(visit);

        // Complete is illegal directly from Waiting.
        var result = await _sut.TransitionAsync(consultation.Id, "complete", actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.InvalidStatusTransition);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransitionAsync_UnknownAction_ReturnsFailure()
    {
        var visit = PatientVisit.Create(PatientId, VisitType.OP, appointmentTypeId: null, createdBy: null);
        visit.AddConsultation(PatientVisitConsultation.Create(visit.Id, DepartmentId, ConsultantId, consultationTypeId: null), updatedBy: null);
        var consultation = visit.Consultations.Single();
        _repository.GetByConsultationIdAsync(consultation.Id, Arg.Any<CancellationToken>()).Returns(visit);

        var result = await _sut.TransitionAsync(consultation.Id, "not-a-real-action", actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.InvalidStatusTransition);
    }
}
