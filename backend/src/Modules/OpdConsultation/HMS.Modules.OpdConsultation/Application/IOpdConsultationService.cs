using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.OpdConsultation.Application.Abstractions;
using HMS.Modules.OpdConsultation.Application.Mapping;
using HMS.Modules.OpdConsultation.Contracts;
using HMS.Modules.OpdConsultation.Domain;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.OpdConsultation.Application;

/// <summary>
/// Public (not internal): OpdConsultationsController requires a public constructor dependency
/// (CS0051 otherwise). Interface and implementation share this file, matching
/// DischargeSummaryService's convention.
/// </summary>
public interface IOpdConsultationService
{
    /// <summary>Fetches the note for a consultation, auto-creating an empty Draft one on first
    /// call — there is no separate "Create" action a user triggers themselves (unlike
    /// DischargeSummary, where creating one is its own deliberate step from a list of
    /// discharged admissions); opening the form is itself the first save point. Fails with
    /// InvalidConsultation if the consultation doesn't exist.</summary>
    Task<Result<OpdConsultationDetailResponse>> GetOrCreateByConsultationIdAsync(Guid consultationId, Guid? actorId, CancellationToken cancellationToken);

    /// <summary>Every consultation note already recorded for a patient, each with the same
    /// header GetOrCreate returns — the Patient Details page's Medical Information tab. Strictly
    /// read-only: unlike GetOrCreate it never creates a note, so browsing a patient's history
    /// can't leave empty drafts behind. A note whose consultation can no longer be resolved is
    /// skipped rather than failing the whole list.</summary>
    Task<Result<IReadOnlyList<OpdConsultationDetailResponse>>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken);

    /// <summary>Full-record save with no required fields. Fails with NotDraft once
    /// Completed.</summary>
    Task<Result<OpdConsultationNoteResponse>> SaveDraftAsync(Guid consultationId, SaveOpdConsultationRequest request, Guid? actorId, CancellationToken cancellationToken);

    /// <summary>Same full-record save as SaveDraft, but requires PresentingComplaints/
    /// HeightCm/WeightKg, and additionally transitions the owning PatientVisitConsultation to
    /// Completed via Patients' public IOpdQueryService — the clinical note and the OPD queue
    /// entry complete together. Fails with AlreadyCompleted if already Completed, or
    /// MissingRequiredFieldsForCompletion if the three fields above aren't all set.</summary>
    Task<Result<OpdConsultationNoteResponse>> CompleteAsync(Guid consultationId, SaveOpdConsultationRequest request, Guid? actorId, CancellationToken cancellationToken);

    /// <summary>The inverse of CompleteAsync — moves a Completed note back to Draft so it can
    /// be edited again, and reverts the owning PatientVisitConsultation's queue status from
    /// Completed back to InConsultation via Patients' public IOpdQueryService, same seam
    /// CompleteAsync uses going the other direction. Fails with NotCompleted if the note isn't
    /// currently Completed. Permission-gated at the controller to clinical-care.edit — the same
    /// gate SaveDraft/Complete already use.</summary>
    Task<Result<OpdConsultationNoteResponse>> ReopenAsync(Guid consultationId, Guid? actorId, CancellationToken cancellationToken);

    /// <summary>Structures a dictated/typed transcript into the note's narrative fields via
    /// IClinicalNoteAiClient — the result is returned for the form to merge into its own draft
    /// state, never saved by this call itself (the existing SaveDraft/Complete calls remain the
    /// only persistence path). Fails with NotFound if no note exists yet (GET first, same as
    /// SaveDraft/Complete), or NotDraft once Completed — matches SaveDraft's guard, since there's
    /// no reason to keep dictating into a note that can no longer be edited.</summary>
    Task<Result<StructuredConsultationNoteResponse>> StructureNoteFromTranscriptAsync(Guid consultationId, string transcript, CancellationToken cancellationToken);

    /// <summary>Active Diagnosis-catalog entries matching a name or ICD code (all active entries,
    /// up to 100, when search is empty) — the consultation form's diagnosis picker (OPD-03).</summary>
    Task<IReadOnlyList<OpdDiagnosisOptionResponse>> SearchDiagnosesAsync(string? search, CancellationToken cancellationToken);

    /// <summary>Adds a diagnosis to the catalog from the consultation form. Returns the existing
    /// entry instead when one with the same name (case-insensitive) already exists, so two doctors
    /// adding the same diagnosis never fail or duplicate it (OPD-03).</summary>
    Task<Result<OpdDiagnosisOptionResponse>> CreateDiagnosisAsync(CreateOpdDiagnosisRequest request, Guid? actorId, CancellationToken cancellationToken);

    /// <summary>Every active Laboratory or Radiology catalog service — the consultation form's
    /// investigation picker (OPD-01).</summary>
    Task<IReadOnlyList<OpdInvestigationServiceOptionResponse>> GetInvestigationServicesAsync(OpdInvestigationDepartment department, CancellationToken cancellationToken);

    /// <summary>The catalog-linked investigations ordered on one visit's consultation notes, for
    /// OPD Billing Entry to pre-add (OPD-01). One entry per distinct service per visit.</summary>
    Task<IReadOnlyList<BillableInvestigationResponse>> GetBillableInvestigationsAsync(Guid visitId, CancellationToken cancellationToken);
}

internal class OpdConsultationService : IOpdConsultationService
{
    private readonly IOpdConsultationRepository _repository;
    private readonly IOpdQueryService _opdQueryService;
    private readonly IDiagnosisService _diagnosisService;
    private readonly IDiagnosticServiceService _diagnosticServiceService;
    private readonly IDepartmentService _departmentService;
    private readonly IConsultantService _consultantService;
    private readonly IClinicalNoteAiClient _clinicalNoteAiClient;

    public OpdConsultationService(
        IOpdConsultationRepository repository,
        IOpdQueryService opdQueryService,
        IDiagnosisService diagnosisService,
        IDiagnosticServiceService diagnosticServiceService,
        IDepartmentService departmentService,
        IConsultantService consultantService,
        IClinicalNoteAiClient clinicalNoteAiClient)
    {
        _repository = repository;
        _opdQueryService = opdQueryService;
        _diagnosisService = diagnosisService;
        _diagnosticServiceService = diagnosticServiceService;
        _departmentService = departmentService;
        _consultantService = consultantService;
        _clinicalNoteAiClient = clinicalNoteAiClient;
    }

    public async Task<Result<OpdConsultationDetailResponse>> GetOrCreateByConsultationIdAsync(Guid consultationId, Guid? actorId, CancellationToken cancellationToken)
    {
        var detailResult = await _opdQueryService.GetConsultationDetailAsync(consultationId, cancellationToken);
        if (!detailResult.IsSuccess)
        {
            return Result<OpdConsultationDetailResponse>.Failure(OpdConsultationErrorCodes.InvalidConsultation, $"Consultation '{consultationId}' was not found.");
        }

        var item = detailResult.Value!;

        var note = await _repository.GetByConsultationIdAsync(consultationId, cancellationToken);
        if (note is null)
        {
            note = OpdConsultationNote.Create(consultationId, item.PatientId, item.VisitId, actorId);
            await _repository.AddAsync(note, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        var header = ToHeader(item);

        return Result<OpdConsultationDetailResponse>.Success(new OpdConsultationDetailResponse { Header = header, Note = await ToResponseAsync(note, cancellationToken) });
    }

    public async Task<Result<IReadOnlyList<OpdConsultationDetailResponse>>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var notes = await _repository.GetByPatientIdAsync(patientId, cancellationToken);

        var details = new List<OpdConsultationDetailResponse>(notes.Count);
        foreach (var note in notes)
        {
            var detailResult = await _opdQueryService.GetConsultationDetailAsync(note.ConsultationId, cancellationToken);
            if (!detailResult.IsSuccess)
            {
                continue;
            }

            details.Add(new OpdConsultationDetailResponse { Header = ToHeader(detailResult.Value!), Note = await ToResponseAsync(note, cancellationToken) });
        }

        return Result<IReadOnlyList<OpdConsultationDetailResponse>>.Success(details);
    }

    public async Task<Result<OpdConsultationNoteResponse>> SaveDraftAsync(Guid consultationId, SaveOpdConsultationRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var note = await _repository.GetByConsultationIdAsync(consultationId, cancellationToken);
        if (note is null)
        {
            return Result<OpdConsultationNoteResponse>.Failure(OpdConsultationErrorCodes.NotFound, $"No consultation note exists for consultation '{consultationId}'.");
        }

        if (note.Status != OpdConsultationNoteStatus.Draft)
        {
            return Result<OpdConsultationNoteResponse>.Failure(OpdConsultationErrorCodes.NotDraft, "This consultation has already been completed and can no longer be edited.");
        }

        var validationFailure = await ValidateReferencesAsync(request, cancellationToken);
        if (validationFailure is not null)
        {
            return Result<OpdConsultationNoteResponse>.Failure(validationFailure.Value.Code, validationFailure.Value.Message);
        }

        ApplyRequest(note, request, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<OpdConsultationNoteResponse>.Success(await ToResponseAsync(note, cancellationToken));
    }

    public async Task<Result<OpdConsultationNoteResponse>> CompleteAsync(Guid consultationId, SaveOpdConsultationRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var note = await _repository.GetByConsultationIdAsync(consultationId, cancellationToken);
        if (note is null)
        {
            return Result<OpdConsultationNoteResponse>.Failure(OpdConsultationErrorCodes.NotFound, $"No consultation note exists for consultation '{consultationId}'.");
        }

        if (note.Status == OpdConsultationNoteStatus.Completed)
        {
            return Result<OpdConsultationNoteResponse>.Failure(OpdConsultationErrorCodes.AlreadyCompleted, $"Consultation '{consultationId}' has already been completed.");
        }

        if (string.IsNullOrWhiteSpace(request.PresentingComplaints) || request.HeightCm is null || request.WeightKg is null)
        {
            return Result<OpdConsultationNoteResponse>.Failure(
                OpdConsultationErrorCodes.MissingRequiredFieldsForCompletion,
                "Presenting complaints, height, and weight are required to complete a consultation.");
        }

        var validationFailure = await ValidateReferencesAsync(request, cancellationToken);
        if (validationFailure is not null)
        {
            return Result<OpdConsultationNoteResponse>.Failure(validationFailure.Value.Code, validationFailure.Value.Message);
        }

        ApplyRequest(note, request, actorId);
        note.Complete(actorId);

        // The clinical note and the OPD queue entry complete together — reuses the existing
        // Waiting/CheckedIn/InConsultation -> Completed status machine already built for the
        // OPD Patient List's "Consult" action, rather than duplicating it.
        var transitionResult = await _opdQueryService.TransitionAsync(consultationId, "complete", actorId, cancellationToken);
        if (!transitionResult.IsSuccess)
        {
            return Result<OpdConsultationNoteResponse>.Failure(transitionResult.ErrorCode!, transitionResult.Error!);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<OpdConsultationNoteResponse>.Success(await ToResponseAsync(note, cancellationToken));
    }

    public async Task<Result<OpdConsultationNoteResponse>> ReopenAsync(Guid consultationId, Guid? actorId, CancellationToken cancellationToken)
    {
        var note = await _repository.GetByConsultationIdAsync(consultationId, cancellationToken);
        if (note is null)
        {
            return Result<OpdConsultationNoteResponse>.Failure(OpdConsultationErrorCodes.NotFound, $"No consultation note exists for consultation '{consultationId}'.");
        }

        if (note.Status != OpdConsultationNoteStatus.Completed)
        {
            return Result<OpdConsultationNoteResponse>.Failure(OpdConsultationErrorCodes.NotCompleted, "Only a completed consultation can be reopened.");
        }

        // The clinical note and the OPD queue entry move back together — mirrors CompleteAsync's
        // own use of this same transition seam going the other direction.
        var transitionResult = await _opdQueryService.TransitionAsync(consultationId, "reopen", actorId, cancellationToken);
        if (!transitionResult.IsSuccess)
        {
            return Result<OpdConsultationNoteResponse>.Failure(transitionResult.ErrorCode!, transitionResult.Error!);
        }

        note.Reopen(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<OpdConsultationNoteResponse>.Success(await ToResponseAsync(note, cancellationToken));
    }

    public async Task<Result<StructuredConsultationNoteResponse>> StructureNoteFromTranscriptAsync(Guid consultationId, string transcript, CancellationToken cancellationToken)
    {
        var note = await _repository.GetByConsultationIdAsync(consultationId, cancellationToken);
        if (note is null)
        {
            return Result<StructuredConsultationNoteResponse>.Failure(OpdConsultationErrorCodes.NotFound, $"No consultation note exists for consultation '{consultationId}'.");
        }

        if (note.Status != OpdConsultationNoteStatus.Draft)
        {
            return Result<StructuredConsultationNoteResponse>.Failure(OpdConsultationErrorCodes.NotDraft, "This consultation has already been completed and can no longer be edited.");
        }

        return await _clinicalNoteAiClient.StructureAsync(transcript, cancellationToken);
    }

    private static OpdConsultationHeader ToHeader(OpdPatientListItem item) => new()
    {
        ConsultationId = item.ConsultationId,
        VisitId = item.VisitId,
        PatientId = item.PatientId,
        Uhid = item.Uhid,
        PatientName = item.PatientName,
        PhoneNumber = item.PhoneNumber,
        Age = item.Age,
        Gender = item.Gender.ToString(),
        AppointmentTime = item.AppointmentTime,
        DepartmentId = item.DepartmentId,
        DepartmentName = item.DepartmentName,
        ConsultantId = item.ConsultantId,
        ConsultantName = item.ConsultantName,
        ConsultationStatus = item.Status.ToString(),
    };

    private void ApplyRequest(OpdConsultationNote note, SaveOpdConsultationRequest request, Guid? actorId)
    {
        note.SaveDetails(
            request.HeightCm,
            request.WeightKg,
            request.PulseRate,
            request.BloodPressure,
            request.TemperatureF,
            request.SpO2Percent,
            request.PresentingComplaints,
            request.ClinicalHistory,
            request.ExaminationFindings,
            request.PlanOfManagement,
            request.ReviewDate,
            request.FollowUpInstructions,
            request.EmergencyReviewInstructions,
            request.ReferralDepartmentId,
            request.ReferralConsultantId,
            request.ReferralReason,
            actorId);

        var diagnoses = request.Diagnoses.Select(d => OpdConsultationDiagnosis.Create(note.Id, d.DiagnosisId, d.Type));
        note.ReplaceDiagnoses(diagnoses, actorId);

        var investigations = request.Investigations.Select(i => OpdConsultationInvestigation.Create(note.Id, i.Name, i.Department, i.Priority, i.ServiceId));
        note.ReplaceInvestigations(investigations, actorId);

        var prescriptions = request.Prescriptions.Select(p => OpdConsultationPrescription.Create(note.Id, p.DrugName, p.Dose, p.Route, p.Frequency, p.DurationDays, p.Instructions));
        note.ReplacePrescriptions(prescriptions, actorId);
    }

    /// <summary>Maps a note to its response with each diagnosis line's catalog name/ICD code
    /// resolved from Masters — the form, print-out and Medical Information tab show names from
    /// here rather than from the admin-only Masters endpoints (OPD-03).</summary>
    private async Task<OpdConsultationNoteResponse> ToResponseAsync(OpdConsultationNote note, CancellationToken cancellationToken)
    {
        var labels = new Dictionary<Guid, (string Name, string? IcdCode)>();
        foreach (var diagnosisId in note.Diagnoses.Select(d => d.DiagnosisId).Distinct())
        {
            var diagnosis = await _diagnosisService.GetByIdAsync(diagnosisId, cancellationToken);
            if (diagnosis is { IsSuccess: true, Value: not null })
            {
                labels[diagnosisId] = (diagnosis.Value.Name, diagnosis.Value.IcdCode);
            }
        }

        return note.ToResponse(labels);
    }

    public async Task<IReadOnlyList<OpdDiagnosisOptionResponse>> SearchDiagnosesAsync(string? search, CancellationToken cancellationToken)
    {
        var page = await _diagnosisService.GetPagedAsync(
            new DiagnosisListQuery { IsActive = true, Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(), PageSize = PagedRequest.MaxPageSize },
            cancellationToken);

        return page.Items.Select(d => new OpdDiagnosisOptionResponse { Id = d.Id, Name = d.Name, IcdCode = d.IcdCode }).ToList();
    }

    public async Task<Result<OpdDiagnosisOptionResponse>> CreateDiagnosisAsync(CreateOpdDiagnosisRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        var existing = await _diagnosisService.GetPagedAsync(new DiagnosisListQuery { Search = name, PageSize = PagedRequest.MaxPageSize }, cancellationToken);
        var match = existing.Items.FirstOrDefault(d => string.Equals(d.Name.Trim(), name, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
        {
            return Result<OpdDiagnosisOptionResponse>.Success(new OpdDiagnosisOptionResponse { Id = match.Id, Name = match.Name, IcdCode = match.IcdCode });
        }

        var icdCode = string.IsNullOrWhiteSpace(request.IcdCode) ? null : request.IcdCode.Trim();
        var created = await _diagnosisService.CreateAsync(new CreateDiagnosisRequest { Name = name, IcdCode = icdCode, IsActive = true }, actorId, cancellationToken);
        if (!created.IsSuccess || created.Value is null)
        {
            return Result<OpdDiagnosisOptionResponse>.Failure(OpdConsultationErrorCodes.InvalidDiagnosis, created.Error ?? "The diagnosis could not be added.");
        }

        return Result<OpdDiagnosisOptionResponse>.Success(new OpdDiagnosisOptionResponse { Id = created.Value.Id, Name = created.Value.Name, IcdCode = created.Value.IcdCode });
    }

    public async Task<IReadOnlyList<OpdInvestigationServiceOptionResponse>> GetInvestigationServicesAsync(OpdInvestigationDepartment department, CancellationToken cancellationToken)
    {
        var serviceType = ToServiceType(department);
        var options = new List<OpdInvestigationServiceOptionResponse>();

        // The catalog runs to a few hundred services, beyond one page (MaxPageSize) — page through
        // it so the picker's client-side search sees every test.
        for (var pageNumber = 1; ; pageNumber++)
        {
            var page = await _diagnosticServiceService.GetPagedAsync(
                new DiagnosticServiceListQuery { ServiceType = serviceType, IsActive = true, Page = pageNumber, PageSize = PagedRequest.MaxPageSize },
                cancellationToken);

            options.AddRange(page.Items.Select(s => new OpdInvestigationServiceOptionResponse { Id = s.Id, Code = s.Code, Name = s.Name, Department = department }));

            if (page.Items.Count < PagedRequest.MaxPageSize || options.Count >= page.TotalCount)
            {
                break;
            }
        }

        return options.OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<IReadOnlyList<BillableInvestigationResponse>> GetBillableInvestigationsAsync(Guid visitId, CancellationToken cancellationToken)
    {
        var notes = await _repository.GetByVisitIdAsync(visitId, cancellationToken);
        var billable = new List<BillableInvestigationResponse>();

        foreach (var note in notes)
        {
            var linked = note.Investigations.Where(i => i.ServiceId.HasValue).ToList();
            if (linked.Count == 0)
            {
                continue;
            }

            var detail = await _opdQueryService.GetConsultationDetailAsync(note.ConsultationId, cancellationToken);
            if (!detail.IsSuccess || detail.Value is null)
            {
                continue;
            }

            foreach (var investigation in linked)
            {
                var serviceId = investigation.ServiceId!.Value;
                if (billable.Any(b => b.ServiceId == serviceId))
                {
                    continue;
                }

                billable.Add(new BillableInvestigationResponse
                {
                    ConsultationId = note.ConsultationId,
                    ConsultantId = detail.Value.ConsultantId,
                    ServiceId = serviceId,
                    Name = investigation.Name,
                    Department = investigation.Department,
                    Priority = investigation.Priority,
                });
            }
        }

        return billable;
    }

    private static DiagnosticTestServiceType ToServiceType(OpdInvestigationDepartment department) => department switch
    {
        OpdInvestigationDepartment.Radiology => DiagnosticTestServiceType.Radiology,
        _ => DiagnosticTestServiceType.Laboratory,
    };

    /// <summary>Validates every cross-module reference the request carries — each Diagnosis
    /// line's DiagnosisId (Masters), and ReferralDepartmentId/ReferralConsultantId if set
    /// (Masters) — returning the first failure found, or null if everything resolves.</summary>
    private async Task<(string Code, string Message)?> ValidateReferencesAsync(SaveOpdConsultationRequest request, CancellationToken cancellationToken)
    {
        foreach (var diagnosisId in request.Diagnoses.Select(d => d.DiagnosisId).Distinct())
        {
            if (!(await _diagnosisService.GetByIdAsync(diagnosisId, cancellationToken)).IsSuccess)
            {
                return (OpdConsultationErrorCodes.InvalidDiagnosis, $"Diagnosis '{diagnosisId}' was not found.");
            }
        }

        // Catalog-linked investigations must point at a real, active service of the line's own
        // department — OPD Billing Entry bills exactly these ids (OPD-01).
        foreach (var investigation in request.Investigations.Where(i => i.ServiceId.HasValue))
        {
            var service = await _diagnosticServiceService.GetByIdAsync(investigation.ServiceId!.Value, cancellationToken);
            if (service is not { IsSuccess: true, Value: not null } || !service.Value.IsActive || service.Value.ServiceType != ToServiceType(investigation.Department))
            {
                return (OpdConsultationErrorCodes.InvalidInvestigation, $"Investigation '{investigation.Name}' does not match an active {investigation.Department} service.");
            }
        }

        if (request.ReferralDepartmentId.HasValue && !(await _departmentService.GetByIdAsync(request.ReferralDepartmentId.Value, cancellationToken)).IsSuccess)
        {
            return (OpdConsultationErrorCodes.InvalidReferral, $"Department '{request.ReferralDepartmentId}' was not found.");
        }

        if (request.ReferralConsultantId.HasValue && !(await _consultantService.GetByIdAsync(request.ReferralConsultantId.Value, cancellationToken)).IsSuccess)
        {
            return (OpdConsultationErrorCodes.InvalidReferral, $"Consultant '{request.ReferralConsultantId}' was not found.");
        }

        return null;
    }
}
