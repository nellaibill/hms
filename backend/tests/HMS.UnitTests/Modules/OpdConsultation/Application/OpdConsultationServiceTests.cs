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
    private readonly IDiagnosticServiceService _diagnosticServiceService = Substitute.For<IDiagnosticServiceService>();
    private readonly IDepartmentService _departmentService = Substitute.For<IDepartmentService>();
    private readonly IConsultantService _consultantService = Substitute.For<IConsultantService>();
    private readonly IClinicalNoteAiClient _clinicalNoteAiClient = Substitute.For<IClinicalNoteAiClient>();
    private readonly IOpdConsultationService _sut;

    public OpdConsultationServiceTests()
    {
        _sut = new OpdConsultationService(_repository, _opdQueryService, _diagnosisService, _diagnosticServiceService, _departmentService, _consultantService, _clinicalNoteAiClient);
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

    [Fact]
    public async Task StructureNoteFromTranscriptAsync_WhenNoNoteExists_ReturnsNotFound()
    {
        var consultationId = Guid.NewGuid();
        _repository.GetByConsultationIdAsync(consultationId, Arg.Any<CancellationToken>()).Returns((OpdConsultationNote?)null);

        var result = await _sut.StructureNoteFromTranscriptAsync(consultationId, "Patient reports knee pain.", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(OpdConsultationErrorCodes.NotFound);
        await _clinicalNoteAiClient.DidNotReceive().StructureAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StructureNoteFromTranscriptAsync_WhenAlreadyCompleted_ReturnsNotDraft()
    {
        var consultationId = Guid.NewGuid();
        var note = OpdConsultationNote.Create(consultationId, Guid.NewGuid(), Guid.NewGuid(), createdBy: null);
        note.Complete(updatedBy: null);
        _repository.GetByConsultationIdAsync(consultationId, Arg.Any<CancellationToken>()).Returns(note);

        var result = await _sut.StructureNoteFromTranscriptAsync(consultationId, "Patient reports knee pain.", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(OpdConsultationErrorCodes.NotDraft);
        await _clinicalNoteAiClient.DidNotReceive().StructureAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StructureNoteFromTranscriptAsync_WhenDraft_DelegatesToTheAiClientAndReturnsItsResult()
    {
        var consultationId = Guid.NewGuid();
        var note = OpdConsultationNote.Create(consultationId, Guid.NewGuid(), Guid.NewGuid(), createdBy: null);
        _repository.GetByConsultationIdAsync(consultationId, Arg.Any<CancellationToken>()).Returns(note);
        var structured = new StructuredConsultationNoteResponse { PresentingComplaints = "Knee pain since 2 weeks." };
        _clinicalNoteAiClient.StructureAsync("Patient reports knee pain.", Arg.Any<CancellationToken>())
            .Returns(Result<StructuredConsultationNoteResponse>.Success(structured));

        var result = await _sut.StructureNoteFromTranscriptAsync(consultationId, "Patient reports knee pain.", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PresentingComplaints.Should().Be("Knee pain since 2 weeks.");
    }

    [Fact]
    public async Task StructureNoteFromTranscriptAsync_WhenTheAiClientFails_ReturnsThatFailure()
    {
        var consultationId = Guid.NewGuid();
        var note = OpdConsultationNote.Create(consultationId, Guid.NewGuid(), Guid.NewGuid(), createdBy: null);
        _repository.GetByConsultationIdAsync(consultationId, Arg.Any<CancellationToken>()).Returns(note);
        _clinicalNoteAiClient.StructureAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<StructuredConsultationNoteResponse>.Failure(OpdConsultationErrorCodes.AiNotConfigured, "not configured"));

        var result = await _sut.StructureNoteFromTranscriptAsync(consultationId, "Patient reports knee pain.", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(OpdConsultationErrorCodes.AiNotConfigured);
    }

    [Fact]
    public async Task GetByPatientIdAsync_ReturnsEachExistingNoteWithItsHeader_AndNeverCreatesOne()
    {
        var patientId = Guid.NewGuid();
        var visitId = Guid.NewGuid();
        var first = OpdConsultationNote.Create(Guid.NewGuid(), patientId, visitId, createdBy: null);
        var second = OpdConsultationNote.Create(Guid.NewGuid(), patientId, visitId, createdBy: null);
        _repository.GetByPatientIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(new[] { first, second });
        _opdQueryService.GetConsultationDetailAsync(first.ConsultationId, Arg.Any<CancellationToken>())
            .Returns(Result<OpdPatientListItem>.Success(NewListItem(first.ConsultationId, patientId, visitId)));
        _opdQueryService.GetConsultationDetailAsync(second.ConsultationId, Arg.Any<CancellationToken>())
            .Returns(Result<OpdPatientListItem>.Success(NewListItem(second.ConsultationId, patientId, visitId)));

        var result = await _sut.GetByPatientIdAsync(patientId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(d => d.Header.ConsultationId).Should().Equal(first.ConsultationId, second.ConsultationId);
        result.Value!.Should().OnlyContain(d => d.Header.PatientId == patientId && d.Header.ConsultantName == "Dr. Test");
        await _repository.DidNotReceive().AddAsync(Arg.Any<OpdConsultationNote>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByPatientIdAsync_SkipsANoteWhoseConsultationCanNoLongerBeResolved()
    {
        var patientId = Guid.NewGuid();
        var resolvable = OpdConsultationNote.Create(Guid.NewGuid(), patientId, Guid.NewGuid(), createdBy: null);
        var orphaned = OpdConsultationNote.Create(Guid.NewGuid(), patientId, Guid.NewGuid(), createdBy: null);
        _repository.GetByPatientIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(new[] { resolvable, orphaned });
        _opdQueryService.GetConsultationDetailAsync(resolvable.ConsultationId, Arg.Any<CancellationToken>())
            .Returns(Result<OpdPatientListItem>.Success(NewListItem(resolvable.ConsultationId, patientId, resolvable.VisitId)));
        _opdQueryService.GetConsultationDetailAsync(orphaned.ConsultationId, Arg.Any<CancellationToken>())
            .Returns(Result<OpdPatientListItem>.Failure(PatientErrorCodes.ConsultationNotFound, "not found"));

        var result = await _sut.GetByPatientIdAsync(patientId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainSingle().Which.Header.ConsultationId.Should().Be(resolvable.ConsultationId);
    }

    private void GivenCatalogService(Guid id, DiagnosticTestServiceType type, bool isActive = true) =>
        _diagnosticServiceService.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticServiceResponse>.Success(new DiagnosticServiceResponse { Id = id, Name = "Complete Blood Count", ServiceType = type, IsActive = isActive }));

    // OPD-02 (prescriptions) + OPD-01 (catalog-linked investigation) + OPD-03 (diagnosis name in
    // the response, so the UI never needs the admin-only Masters endpoints).
    [Fact]
    public async Task SaveDraftAsync_WithPrescriptionsAndACatalogInvestigation_PersistsThemAndResolvesTheDiagnosisName()
    {
        var consultationId = Guid.NewGuid();
        var note = OpdConsultationNote.Create(consultationId, Guid.NewGuid(), Guid.NewGuid(), createdBy: null);
        _repository.GetByConsultationIdAsync(consultationId, Arg.Any<CancellationToken>()).Returns(note);
        var diagnosisId = Guid.NewGuid();
        _diagnosisService.GetByIdAsync(diagnosisId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosisResponse>.Success(new DiagnosisResponse { Id = diagnosisId, Name = "Acute otitis media", IcdCode = "H66.9" }));
        var serviceId = Guid.NewGuid();
        GivenCatalogService(serviceId, DiagnosticTestServiceType.Laboratory);

        var request = new SaveOpdConsultationRequest
        {
            Diagnoses = [new OpdConsultationDiagnosisRequest { DiagnosisId = diagnosisId, Type = OpdDiagnosisType.Primary }],
            Investigations = [new OpdConsultationInvestigationRequest { Name = "Complete Blood Count", Department = OpdInvestigationDepartment.Laboratory, Priority = OpdInvestigationPriority.Routine, ServiceId = serviceId }],
            Prescriptions =
            [
                new OpdConsultationPrescriptionRequest { DrugName = " Amoxicillin 500 mg ", Dose = "1 cap", Route = "Oral", Frequency = "1-1-1", DurationDays = 5, Instructions = "After food" },
            ],
        };

        var result = await _sut.SaveDraftAsync(consultationId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var diagnosis = result.Value!.Diagnoses.Should().ContainSingle().Subject;
        diagnosis.DiagnosisName.Should().Be("Acute otitis media");
        diagnosis.IcdCode.Should().Be("H66.9");
        result.Value.Investigations.Should().ContainSingle().Which.ServiceId.Should().Be(serviceId);
        var prescription = result.Value.Prescriptions.Should().ContainSingle().Subject;
        prescription.DrugName.Should().Be("Amoxicillin 500 mg");
        prescription.Frequency.Should().Be("1-1-1");
        prescription.DurationDays.Should().Be(5);
    }

    [Fact]
    public async Task SaveDraftAsync_WithAnInvestigationServiceIdThatDoesNotExist_ReturnsInvalidInvestigation()
    {
        var consultationId = Guid.NewGuid();
        var note = OpdConsultationNote.Create(consultationId, Guid.NewGuid(), Guid.NewGuid(), createdBy: null);
        _repository.GetByConsultationIdAsync(consultationId, Arg.Any<CancellationToken>()).Returns(note);
        var serviceId = Guid.NewGuid();
        _diagnosticServiceService.GetByIdAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticServiceResponse>.Failure(MastersErrorCodes.NotFound, "not found"));

        var request = new SaveOpdConsultationRequest
        {
            Investigations = [new OpdConsultationInvestigationRequest { Name = "CBC", Department = OpdInvestigationDepartment.Laboratory, ServiceId = serviceId }],
        };

        var result = await _sut.SaveDraftAsync(consultationId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(OpdConsultationErrorCodes.InvalidInvestigation);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveDraftAsync_WithARadiologyServiceOnALaboratoryLine_ReturnsInvalidInvestigation()
    {
        var consultationId = Guid.NewGuid();
        var note = OpdConsultationNote.Create(consultationId, Guid.NewGuid(), Guid.NewGuid(), createdBy: null);
        _repository.GetByConsultationIdAsync(consultationId, Arg.Any<CancellationToken>()).Returns(note);
        var serviceId = Guid.NewGuid();
        GivenCatalogService(serviceId, DiagnosticTestServiceType.Radiology);

        var request = new SaveOpdConsultationRequest
        {
            Investigations = [new OpdConsultationInvestigationRequest { Name = "X-Ray Chest", Department = OpdInvestigationDepartment.Laboratory, ServiceId = serviceId }],
        };

        var result = await _sut.SaveDraftAsync(consultationId, request, actorId: null, CancellationToken.None);

        result.ErrorCode.Should().Be(OpdConsultationErrorCodes.InvalidInvestigation);
    }

    [Fact]
    public async Task CreateDiagnosisAsync_WhenTheNameAlreadyExists_ReturnsTheExistingEntryWithoutCreating()
    {
        var existingId = Guid.NewGuid();
        _diagnosisService.GetPagedAsync(Arg.Any<DiagnosisListQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<DiagnosisResponse>([new DiagnosisResponse { Id = existingId, Name = "Acute Otitis Media", IcdCode = "H66.9" }], 1, 100, 1));

        var result = await _sut.CreateDiagnosisAsync(new CreateOpdDiagnosisRequest { Name = "  acute otitis media " }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(existingId);
        await _diagnosisService.DidNotReceive().CreateAsync(Arg.Any<CreateDiagnosisRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDiagnosisAsync_WhenNoMatchExists_CreatesAnActiveCatalogEntry()
    {
        _diagnosisService.GetPagedAsync(Arg.Any<DiagnosisListQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<DiagnosisResponse>([], 1, 100, 0));
        var createdId = Guid.NewGuid();
        _diagnosisService.CreateAsync(Arg.Any<CreateDiagnosisRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosisResponse>.Success(new DiagnosisResponse { Id = createdId, Name = "Impacted cerumen", IcdCode = "H61.2" }));

        var result = await _sut.CreateDiagnosisAsync(new CreateOpdDiagnosisRequest { Name = "Impacted cerumen", IcdCode = " H61.2 " }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(createdId);
        await _diagnosisService.Received(1).CreateAsync(
            Arg.Is<CreateDiagnosisRequest>(r => r.Name == "Impacted cerumen" && r.IcdCode == "H61.2" && r.IsActive),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchDiagnosesAsync_QueriesOnlyActiveEntriesWithTheSearchTerm()
    {
        _diagnosisService.GetPagedAsync(Arg.Any<DiagnosisListQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<DiagnosisResponse>([new DiagnosisResponse { Id = Guid.NewGuid(), Name = "Acute otitis media", IcdCode = "H66.9" }], 1, 100, 1));

        var result = await _sut.SearchDiagnosesAsync(" H66 ", CancellationToken.None);

        result.Should().ContainSingle().Which.IcdCode.Should().Be("H66.9");
        await _diagnosisService.Received(1).GetPagedAsync(Arg.Is<DiagnosisListQuery>(q => q.IsActive == true && q.Search == "H66"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetBillableInvestigationsAsync_ReturnsEachCatalogLinkedServiceOnceWithTheOrderingConsultant()
    {
        var visitId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var first = OpdConsultationNote.Create(Guid.NewGuid(), patientId, visitId, createdBy: null);
        first.ReplaceInvestigations(
            [
                OpdConsultationInvestigation.Create(first.Id, "Complete Blood Count", OpdInvestigationDepartment.Laboratory, OpdInvestigationPriority.Urgent, serviceId),
                OpdConsultationInvestigation.Create(first.Id, "Free-text test", OpdInvestigationDepartment.Laboratory, OpdInvestigationPriority.Routine),
            ],
            updatedBy: null);
        var second = OpdConsultationNote.Create(Guid.NewGuid(), patientId, visitId, createdBy: null);
        second.ReplaceInvestigations([OpdConsultationInvestigation.Create(second.Id, "Complete Blood Count", OpdInvestigationDepartment.Laboratory, OpdInvestigationPriority.Routine, serviceId)], updatedBy: null);
        _repository.GetByVisitIdAsync(visitId, Arg.Any<CancellationToken>()).Returns(new[] { first, second });
        var firstItem = NewListItem(first.ConsultationId, patientId, visitId);
        _opdQueryService.GetConsultationDetailAsync(first.ConsultationId, Arg.Any<CancellationToken>()).Returns(Result<OpdPatientListItem>.Success(firstItem));
        _opdQueryService.GetConsultationDetailAsync(second.ConsultationId, Arg.Any<CancellationToken>())
            .Returns(Result<OpdPatientListItem>.Success(NewListItem(second.ConsultationId, patientId, visitId)));

        var result = await _sut.GetBillableInvestigationsAsync(visitId, CancellationToken.None);

        var line = result.Should().ContainSingle().Subject;
        line.ServiceId.Should().Be(serviceId);
        line.ConsultantId.Should().Be(firstItem.ConsultantId);
        line.Priority.Should().Be(OpdInvestigationPriority.Urgent);
    }
}
