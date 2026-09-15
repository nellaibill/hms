using FluentAssertions;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.OpdConsultation.Application;
using HMS.Modules.OpdConsultation.Application.Abstractions;
using HMS.Modules.OpdConsultation.Contracts;
using HMS.Modules.OpdConsultation.Domain;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Contracts;
using HMS.Shared.Kernel;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.OpdConsultation.Application;

public class OpdConsultationServiceTests
{
    private readonly IOpdConsultationRepository _repository = Substitute.For<IOpdConsultationRepository>();
    private readonly IOpdQueryService _opdQueryService = Substitute.For<IOpdQueryService>();
    private readonly IDiagnosisService _diagnosisService = Substitute.For<IDiagnosisService>();
    private readonly IDepartmentService _departmentService = Substitute.For<IDepartmentService>();
    private readonly IConsultantService _consultantService = Substitute.For<IConsultantService>();
    private readonly IOpdConsultationService _sut;

    public OpdConsultationServiceTests()
    {
        _sut = new OpdConsultationService(_repository, _opdQueryService, _diagnosisService, _departmentService, _consultantService);
    }

    private static OpdPatientListItem NewListItem(Guid consultationId, Guid patientId, Guid visitId) => new()
    {
        ConsultationId = consultationId,
        VisitId = visitId,
        PatientId = patientId,
        Uhid = "P-2026-000001",
        PatientName = "Test Patient",
        PhoneNumber = "9000000000",
        Age = 30,
        Gender = Gender.Female,
        AppointmentTime = DateTime.UtcNow,
        DepartmentId = Guid.NewGuid(),
        DepartmentName = "General Medicine",
        ConsultantId = Guid.NewGuid(),
        ConsultantName = "Dr. Test",
        Status = OpdConsultationStatus.InConsultation,
    };

    [Fact]
    public async Task GetOrCreateByConsultationIdAsync_WhenConsultationDoesNotExist_ReturnsFailure()
    {
        var consultationId = Guid.NewGuid();
        _opdQueryService.GetConsultationDetailAsync(consultationId, Arg.Any<CancellationToken>())
            .Returns(Result<OpdPatientListItem>.Failure(PatientErrorCodes.ConsultationNotFound, "not found"));

        var result = await _sut.GetOrCreateByConsultationIdAsync(consultationId, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(OpdConsultationErrorCodes.InvalidConsultation);
    }

    [Fact]
    public async Task GetOrCreateByConsultationIdAsync_WhenNoNoteExistsYet_CreatesAndPersistsAnEmptyDraft()
    {
        var consultationId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var visitId = Guid.NewGuid();
        _opdQueryService.GetConsultationDetailAsync(consultationId, Arg.Any<CancellationToken>())
            .Returns(Result<OpdPatientListItem>.Success(NewListItem(consultationId, patientId, visitId)));
        _repository.GetByConsultationIdAsync(consultationId, Arg.Any<CancellationToken>()).Returns((OpdConsultationNote?)null);

        var result = await _sut.GetOrCreateByConsultationIdAsync(consultationId, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Note.Status.Should().Be(OpdConsultationNoteStatus.Draft);
        result.Value!.Header.PatientName.Should().Be("Test Patient");
        await _repository.Received(1).AddAsync(Arg.Any<OpdConsultationNote>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrCreateByConsultationIdAsync_WhenNoteAlreadyExists_DoesNotCreateAnother()
    {
        var consultationId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var visitId = Guid.NewGuid();
        _opdQueryService.GetConsultationDetailAsync(consultationId, Arg.Any<CancellationToken>())
            .Returns(Result<OpdPatientListItem>.Success(NewListItem(consultationId, patientId, visitId)));
        var existing = OpdConsultationNote.Create(consultationId, patientId, visitId, createdBy: null);
        _repository.GetByConsultationIdAsync(consultationId, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _sut.GetOrCreateByConsultationIdAsync(consultationId, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.DidNotReceive().AddAsync(Arg.Any<OpdConsultationNote>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveDraftAsync_WhenNoNoteExists_ReturnsNotFound()
    {
        var consultationId = Guid.NewGuid();
        _repository.GetByConsultationIdAsync(consultationId, Arg.Any<CancellationToken>()).Returns((OpdConsultationNote?)null);

        var result = await _sut.SaveDraftAsync(consultationId, new SaveOpdConsultationRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(OpdConsultationErrorCodes.NotFound);
    }

    [Fact]
    public async Task SaveDraftAsync_WhenAlreadyCompleted_ReturnsNotDraft()
    {
        var consultationId = Guid.NewGuid();
        var note = OpdConsultationNote.Create(consultationId, Guid.NewGuid(), Guid.NewGuid(), createdBy: null);
        note.Complete(updatedBy: null);
        _repository.GetByConsultationIdAsync(consultationId, Arg.Any<CancellationToken>()).Returns(note);

        var result = await _sut.SaveDraftAsync(consultationId, new SaveOpdConsultationRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(OpdConsultationErrorCodes.NotDraft);
    }

    [Fact]
    public async Task SaveDraftAsync_WithAnInvalidDiagnosisId_ReturnsFailure()
    {
        var consultationId = Guid.NewGuid();
        var note = OpdConsultationNote.Create(consultationId, Guid.NewGuid(), Guid.NewGuid(), createdBy: null);
        _repository.GetByConsultationIdAsync(consultationId, Arg.Any<CancellationToken>()).Returns(note);
        var diagnosisId = Guid.NewGuid();
        _diagnosisService.GetByIdAsync(diagnosisId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosisResponse>.Failure(MastersErrorCodes.NotFound, "not found"));

        var request = new SaveOpdConsultationRequest
        {
            Diagnoses = [new OpdConsultationDiagnosisRequest { DiagnosisId = diagnosisId, Type = OpdDiagnosisType.Primary }],
        };

        var result = await _sut.SaveDraftAsync(consultationId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(OpdConsultationErrorCodes.InvalidDiagnosis);
    }

    [Fact]
    public async Task SaveDraftAsync_WithValidData_PersistsVitalsAndDiagnosesAndInvestigations()
    {
        var consultationId = Guid.NewGuid();
        var note = OpdConsultationNote.Create(consultationId, Guid.NewGuid(), Guid.NewGuid(), createdBy: null);
        _repository.GetByConsultationIdAsync(consultationId, Arg.Any<CancellationToken>()).Returns(note);
        var diagnosisId = Guid.NewGuid();
        _diagnosisService.GetByIdAsync(diagnosisId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosisResponse>.Success(new DiagnosisResponse { Id = diagnosisId, Name = "Osteoarthritis of knee" }));

        var request = new SaveOpdConsultationRequest
        {
            HeightCm = 165,
            WeightKg = 62,
            PresentingComplaints = "Knee pain since 2 weeks.",
            Diagnoses = [new OpdConsultationDiagnosisRequest { DiagnosisId = diagnosisId, Type = OpdDiagnosisType.Primary }],
            Investigations = [new OpdConsultationInvestigationRequest { Name = "CBC", Department = OpdInvestigationDepartment.Laboratory, Priority = OpdInvestigationPriority.Routine }],
        };

        var result = await _sut.SaveDraftAsync(consultationId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(OpdConsultationNoteStatus.Draft);
        result.Value!.HeightCm.Should().Be(165);
        result.Value!.Diagnoses.Should().ContainSingle().Which.DiagnosisId.Should().Be(diagnosisId);
        result.Value!.Investigations.Should().ContainSingle().Which.Name.Should().Be("CBC");
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteAsync_WithoutRequiredFields_ReturnsMissingRequiredFields()
    {
        var consultationId = Guid.NewGuid();
        var note = OpdConsultationNote.Create(consultationId, Guid.NewGuid(), Guid.NewGuid(), createdBy: null);
        _repository.GetByConsultationIdAsync(consultationId, Arg.Any<CancellationToken>()).Returns(note);

        var result = await _sut.CompleteAsync(consultationId, new SaveOpdConsultationRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(OpdConsultationErrorCodes.MissingRequiredFieldsForCompletion);
    }

    [Fact]
    public async Task CompleteAsync_WhenAlreadyCompleted_ReturnsAlreadyCompleted()
    {
        var consultationId = Guid.NewGuid();
        var note = OpdConsultationNote.Create(consultationId, Guid.NewGuid(), Guid.NewGuid(), createdBy: null);
        note.Complete(updatedBy: null);
        _repository.GetByConsultationIdAsync(consultationId, Arg.Any<CancellationToken>()).Returns(note);

        var request = new SaveOpdConsultationRequest { PresentingComplaints = "x", HeightCm = 1, WeightKg = 1 };
        var result = await _sut.CompleteAsync(consultationId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(OpdConsultationErrorCodes.AlreadyCompleted);
    }

    [Fact]
    public async Task CompleteAsync_WithRequiredFields_CompletesAndTransitionsTheOwningConsultation()
    {
        var consultationId = Guid.NewGuid();
        var note = OpdConsultationNote.Create(consultationId, Guid.NewGuid(), Guid.NewGuid(), createdBy: null);
        _repository.GetByConsultationIdAsync(consultationId, Arg.Any<CancellationToken>()).Returns(note);
        _opdQueryService.TransitionAsync(consultationId, "complete", Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<VisitConsultationResponse>.Success(new VisitConsultationResponse()));

        var request = new SaveOpdConsultationRequest { PresentingComplaints = "Knee pain.", HeightCm = 165, WeightKg = 62 };
        var result = await _sut.CompleteAsync(consultationId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(OpdConsultationNoteStatus.Completed);
        await _opdQueryService.Received(1).TransitionAsync(consultationId, "complete", Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteAsync_WhenTheOwningConsultationTransitionFails_ReturnsThatFailure()
    {
        var consultationId = Guid.NewGuid();
        var note = OpdConsultationNote.Create(consultationId, Guid.NewGuid(), Guid.NewGuid(), createdBy: null);
        _repository.GetByConsultationIdAsync(consultationId, Arg.Any<CancellationToken>()).Returns(note);
        _opdQueryService.TransitionAsync(consultationId, "complete", Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<VisitConsultationResponse>.Failure(PatientErrorCodes.InvalidStatusTransition, "already completed"));

        var request = new SaveOpdConsultationRequest { PresentingComplaints = "Knee pain.", HeightCm = 165, WeightKg = 62 };
        var result = await _sut.CompleteAsync(consultationId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.InvalidStatusTransition);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReopenAsync_WhenNoteDoesNotExist_ReturnsNotFound()
    {
        var consultationId = Guid.NewGuid();
        _repository.GetByConsultationIdAsync(consultationId, Arg.Any<CancellationToken>()).Returns((OpdConsultationNote?)null);

        var result = await _sut.ReopenAsync(consultationId, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(OpdConsultationErrorCodes.NotFound);
    }

    [Fact]
    public async Task ReopenAsync_WhenNoteIsStillDraft_ReturnsNotCompleted()
    {
        var consultationId = Guid.NewGuid();
        var note = OpdConsultationNote.Create(consultationId, Guid.NewGuid(), Guid.NewGuid(), createdBy: null);
        _repository.GetByConsultationIdAsync(consultationId, Arg.Any<CancellationToken>()).Returns(note);

        var result = await _sut.ReopenAsync(consultationId, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(OpdConsultationErrorCodes.NotCompleted);
    }

    [Fact]
    public async Task ReopenAsync_WhenCompleted_MovesBackToDraftAndTransitionsTheOwningConsultation()
    {
        var consultationId = Guid.NewGuid();
        var note = OpdConsultationNote.Create(consultationId, Guid.NewGuid(), Guid.NewGuid(), createdBy: null);
        note.Complete(updatedBy: null);
        _repository.GetByConsultationIdAsync(consultationId, Arg.Any<CancellationToken>()).Returns(note);
        _opdQueryService.TransitionAsync(consultationId, "reopen", Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<VisitConsultationResponse>.Success(new VisitConsultationResponse()));

        var result = await _sut.ReopenAsync(consultationId, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(OpdConsultationNoteStatus.Draft);
        await _opdQueryService.Received(1).TransitionAsync(consultationId, "reopen", Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReopenAsync_WhenTheOwningConsultationTransitionFails_ReturnsThatFailure()
    {
        var consultationId = Guid.NewGuid();
        var note = OpdConsultationNote.Create(consultationId, Guid.NewGuid(), Guid.NewGuid(), createdBy: null);
        note.Complete(updatedBy: null);
        _repository.GetByConsultationIdAsync(consultationId, Arg.Any<CancellationToken>()).Returns(note);
        _opdQueryService.TransitionAsync(consultationId, "reopen", Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<VisitConsultationResponse>.Failure(PatientErrorCodes.InvalidStatusTransition, "not completed"));

        var result = await _sut.ReopenAsync(consultationId, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.InvalidStatusTransition);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
