using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.OpdConsultation.Application;
using HMS.Modules.OpdConsultation.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.OpdConsultation.Endpoints;

/// <summary>
/// The OPD Consultation clinical form's backing endpoints — reached after the OPD Patient
/// List's "Consult" action (see docs/DecisionLog.md). Uses the "clinical-care" permission
/// group, the same one the OPD nav leaf already declares — this is additive clinical
/// documentation for the same workflow, not a separate RBAC concern.
/// </summary>
[ApiController]
[Route("api/v1/opd-consultations")]
public class OpdConsultationsController : ControllerBase
{
    private readonly IOpdConsultationService _service;
    private readonly IValidator<SaveOpdConsultationRequest> _saveValidator;
    private readonly IValidator<StructureConsultationNoteRequest> _structureNoteValidator;
    private readonly IValidator<CreateOpdDiagnosisRequest> _createDiagnosisValidator;
    private readonly IValidator<MarkPatientRequest> _markPatientValidator;

    public OpdConsultationsController(
        IOpdConsultationService service,
        IValidator<SaveOpdConsultationRequest> saveValidator,
        IValidator<StructureConsultationNoteRequest> structureNoteValidator,
        IValidator<CreateOpdDiagnosisRequest> createDiagnosisValidator,
        IValidator<MarkPatientRequest> markPatientValidator)
    {
        _service = service;
        _saveValidator = saveValidator;
        _structureNoteValidator = structureNoteValidator;
        _createDiagnosisValidator = createDiagnosisValidator;
        _markPatientValidator = markPatientValidator;
    }

    /// <summary>Fetches (auto-creating on first call) the consultation note plus its read-only
    /// patient/appointment/consultant/department header.</summary>
    /// <response code="200">The consultation note and header.</response>
    /// <response code="404">No such consultation exists.</response>
    [Authorize]
    [RequirePermission("clinical-care.view")]
    [HttpGet("{consultationId:guid}")]
    public async Task<IActionResult> GetOrCreate(Guid consultationId, CancellationToken cancellationToken)
    {
        var result = await _service.GetOrCreateByConsultationIdAsync(consultationId, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapDetailFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Lists every consultation note already recorded for one patient (newest first),
    /// each with its header — read-only, never auto-creates a note (unlike GET by consultation).</summary>
    /// <response code="200">The patient's consultation notes (empty when none exist).</response>
    /// <response code="400">patientId is missing.</response>
    [Authorize]
    [RequirePermission("clinical-care.view")]
    [HttpGet]
    public async Task<IActionResult> GetByPatient([FromQuery] Guid patientId, CancellationToken cancellationToken)
    {
        if (patientId == Guid.Empty)
        {
            return BadRequest(new ApiErrorResponse
            {
                ErrorCode = "VALIDATION.FAILED",
                Message = "patientId is required.",
                CorrelationId = HttpContext.GetCorrelationId(),
                Timestamp = DateTime.UtcNow,
            });
        }

        var result = await _service.GetByPatientIdAsync(patientId, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<OpdConsultationDetailResponse>> { Data = result.Value });
    }

    /// <summary>The login types (HMS.Modules.Identity's LoginTypes) the OPD page's Scheduled
    /// Visits and Marked Patients lists are for: the front desk and administrators see every
    /// consultant's, a Consultant only their own.</summary>
    private static readonly HashSet<string> OpdListLoginTypes = new(StringComparer.OrdinalIgnoreCase) { "superAdmin", "admin", "receptionist", "doctor" };

    /// <summary>The OPD page's Scheduled Visits list — every patient's upcoming review dates
    /// (set on their OPD consultations), soonest first. For Receptionists, Admins, Super Admins
    /// and Consultants only; a Consultant only ever sees their own patients here, whatever
    /// consultantId they ask for (same rule as OpdController.GetPatientList).</summary>
    /// <response code="200">The upcoming scheduled visits (empty when none).</response>
    /// <response code="403">The caller's role isn't one this list is for.</response>
    [Authorize]
    [RequirePermission("clinical-care.view")]
    [HttpGet("scheduled-visits")]
    public async Task<IActionResult> GetScheduledVisits([FromQuery] ScheduledVisitQuery query, CancellationToken cancellationToken)
    {
        if (!OpdListLoginTypes.Contains(User.FindFirst("LoginType")?.Value ?? string.Empty))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse
            {
                ErrorCode = OpdConsultationErrorCodes.ScheduledVisitsForbidden,
                Message = "Scheduled visits are only available to Receptionists, Admins, Super Admins and Consultants.",
                CorrelationId = HttpContext.GetCorrelationId(),
                Timestamp = DateTime.UtcNow,
            });
        }

        var scopedConsultantId = User.GetScopedConsultantId();
        if (scopedConsultantId is not null)
        {
            query = query with { ConsultantId = scopedConsultantId };
        }

        var result = await _service.GetScheduledVisitsAsync(query, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<ScheduledVisitResponse>> { Data = result });
    }

    /// <summary>The OPD page's Marked Patients list — every consultation whose patient a doctor
    /// marked, newest mark first. Same audience and Consultant scoping as GetScheduledVisits.</summary>
    /// <response code="200">The marked patients (empty when none).</response>
    /// <response code="403">The caller's role isn't one this list is for.</response>
    [Authorize]
    [RequirePermission("clinical-care.view")]
    [HttpGet("marked-patients")]
    public async Task<IActionResult> GetMarkedPatients([FromQuery] MarkedPatientQuery query, CancellationToken cancellationToken)
    {
        if (!OpdListLoginTypes.Contains(User.FindFirst("LoginType")?.Value ?? string.Empty))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse
            {
                ErrorCode = OpdConsultationErrorCodes.MarkedPatientsForbidden,
                Message = "Marked patients are only available to Receptionists, Admins, Super Admins and Consultants.",
                CorrelationId = HttpContext.GetCorrelationId(),
                Timestamp = DateTime.UtcNow,
            });
        }

        var scopedConsultantId = User.GetScopedConsultantId();
        if (scopedConsultantId is not null)
        {
            query = query with { ConsultantId = scopedConsultantId };
        }

        var result = await _service.GetMarkedPatientsAsync(query, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<MarkedPatientResponse>> { Data = result });
    }

    /// <summary>Marks the consultation's patient, with an optional reason — or updates the
    /// reason when already marked. Works on a Draft or Completed consultation.</summary>
    /// <response code="200">The patient was marked.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No consultation note exists for this consultation yet — GET first.</response>
    [Authorize]
    [RequirePermission("clinical-care.edit")]
    [HttpPost("{consultationId:guid}/mark")]
    public async Task<IActionResult> MarkPatient(Guid consultationId, [FromBody] MarkPatientRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _markPatientValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.MarkPatientAsync(consultationId, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapNoteFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Clears the consultation's mark.</summary>
    /// <response code="200">The mark was removed (or there was none).</response>
    /// <response code="404">No consultation note exists for this consultation yet — GET first.</response>
    [Authorize]
    [RequirePermission("clinical-care.edit")]
    [HttpDelete("{consultationId:guid}/mark")]
    public async Task<IActionResult> UnmarkPatient(Guid consultationId, CancellationToken cancellationToken)
    {
        var result = await _service.UnmarkPatientAsync(consultationId, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapNoteFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Saves the current form state with no required fields.</summary>
    /// <response code="200">The consultation note was saved.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No consultation note exists for this consultation yet — GET first.</response>
    [Authorize]
    [RequirePermission("clinical-care.edit")]
    [HttpPost("{consultationId:guid}/draft")]
    public async Task<IActionResult> SaveDraft(Guid consultationId, [FromBody] SaveOpdConsultationRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _saveValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.SaveDraftAsync(consultationId, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapNoteFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Saves the form and marks the consultation Completed — requires
    /// PresentingComplaints/HeightCm/WeightKg, and also advances the owning
    /// PatientVisitConsultation's own queue status.</summary>
    /// <response code="200">The consultation was completed.</response>
    /// <response code="400">The request failed validation, or a required field is missing.</response>
    /// <response code="404">No consultation note exists for this consultation yet — GET first.</response>
    [Authorize]
    [RequirePermission("clinical-care.edit")]
    [HttpPost("{consultationId:guid}/complete")]
    public async Task<IActionResult> Complete(Guid consultationId, [FromBody] SaveOpdConsultationRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _saveValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.CompleteAsync(consultationId, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapNoteFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Reopens a Completed consultation back to Draft so it can be edited again, and
    /// reverts the owning PatientVisitConsultation's queue status accordingly. Gated by the
    /// same clinical-care.edit permission as SaveDraft/Complete — the consultant, or any role
    /// granted broader clinical-care permissions such as an administrator.</summary>
    /// <response code="200">The consultation was reopened.</response>
    /// <response code="404">No consultation note exists for this consultation.</response>
    /// <response code="409">The consultation isn't currently Completed.</response>
    [Authorize]
    [RequirePermission("clinical-care.edit")]
    [HttpPost("{consultationId:guid}/reopen")]
    public async Task<IActionResult> Reopen(Guid consultationId, CancellationToken cancellationToken)
    {
        var result = await _service.ReopenAsync(consultationId, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapNoteFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Structures a dictated/typed transcript into the note's narrative fields — the
    /// result isn't saved by this call; the client merges it into the form and still submits
    /// through SaveDraft/Complete as normal.</summary>
    /// <response code="200">The structured note fields.</response>
    /// <response code="400">The request failed validation, or the AI provider rejected the
    /// call.</response>
    /// <response code="404">No consultation note exists for this consultation yet — GET first.</response>
    /// <response code="409">The consultation has already been completed.</response>
    /// <response code="503">AI note generation isn't configured for this environment.</response>
    [Authorize]
    [RequirePermission("clinical-care.edit")]
    [RequireFeature("opd-ambient-notes")]
    [HttpPost("{consultationId:guid}/ai/structure-note")]
    public async Task<IActionResult> StructureNote(Guid consultationId, [FromBody] StructureConsultationNoteRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _structureNoteValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.StructureNoteFromTranscriptAsync(consultationId, request.Transcript, cancellationToken);
        return result.IsSuccess ? Ok(new ApiResponse<StructuredConsultationNoteResponse> { Data = result.Value }) : MapStructureNoteFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Active Diagnosis-catalog entries for the consultation form's picker, matching a
    /// name or ICD code. Served here under clinical-care.view because the Masters diagnosis
    /// endpoints require identity-administration, which a doctor doesn't have (OPD-03).</summary>
    /// <response code="200">Matching diagnoses (up to 100).</response>
    [Authorize]
    [RequirePermission("clinical-care.view")]
    [HttpGet("diagnoses")]
    public async Task<IActionResult> SearchDiagnoses([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var result = await _service.SearchDiagnosesAsync(search, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<OpdDiagnosisOptionResponse>> { Data = result });
    }

    /// <summary>Adds a diagnosis to the catalog from the consultation form when the search finds
    /// no match (OPD-03). Returns the existing entry if one with the same name already exists.</summary>
    /// <response code="200">The created (or already existing) diagnosis.</response>
    /// <response code="400">The request failed validation.</response>
    [Authorize]
    [RequirePermission("clinical-care.edit")]
    [HttpPost("diagnoses")]
    public async Task<IActionResult> CreateDiagnosis([FromBody] CreateOpdDiagnosisRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _createDiagnosisValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.CreateDiagnosisAsync(request, actorId: User.GetUserId(), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new ApiErrorResponse { ErrorCode = result.ErrorCode!, Message = result.Error!, CorrelationId = HttpContext.GetCorrelationId(), Timestamp = DateTime.UtcNow });
        }

        return Ok(new ApiResponse<OpdDiagnosisOptionResponse> { Data = result.Value });
    }

    /// <summary>Every active Laboratory or Radiology catalog service, for the consultation form's
    /// investigation picker (OPD-01) — the Masters catalog endpoints require diagnostics.view.</summary>
    /// <response code="200">The department's active services, by name.</response>
    [Authorize]
    [RequirePermission("clinical-care.view")]
    [HttpGet("investigation-services")]
    public async Task<IActionResult> GetInvestigationServices([FromQuery] OpdInvestigationDepartment department, CancellationToken cancellationToken)
    {
        var result = await _service.GetInvestigationServicesAsync(department, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<OpdInvestigationServiceOptionResponse>> { Data = result });
    }

    /// <summary>The catalog-linked investigations the doctor ordered on one visit — OPD Billing
    /// Entry pre-adds these as Laboratory/Radiology lines (OPD-01). Gated on finance-billing.view,
    /// not clinical-care: it's the billing counter that reads this, and it exposes only test
    /// names, never the clinical note itself.</summary>
    /// <response code="200">The visit's billable investigations (empty when none).</response>
    /// <response code="400">visitId is missing.</response>
    [Authorize]
    [RequirePermission("finance-billing.view")]
    [HttpGet("billable-investigations")]
    public async Task<IActionResult> GetBillableInvestigations([FromQuery] Guid visitId, CancellationToken cancellationToken)
    {
        if (visitId == Guid.Empty)
        {
            return BadRequest(new ApiErrorResponse
            {
                ErrorCode = "VALIDATION.FAILED",
                Message = "visitId is required.",
                CorrelationId = HttpContext.GetCorrelationId(),
                Timestamp = DateTime.UtcNow,
            });
        }

        var result = await _service.GetBillableInvestigationsAsync(visitId, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<BillableInvestigationResponse>> { Data = result });
    }

    private static ApiResponse<OpdConsultationDetailResponse> Envelope(OpdConsultationDetailResponse? data) => new() { Data = data };

    private static ApiResponse<OpdConsultationNoteResponse> Envelope(OpdConsultationNoteResponse? data) => new() { Data = data };

    private IActionResult MapDetailFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            OpdConsultationErrorCodes.InvalidConsultation => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest,
        };

        var error = new ApiErrorResponse { ErrorCode = errorCode, Message = message, CorrelationId = HttpContext.GetCorrelationId(), Timestamp = DateTime.UtcNow };
        return StatusCode(status, error);
    }

    private IActionResult MapNoteFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            OpdConsultationErrorCodes.NotFound => StatusCodes.Status404NotFound,
            OpdConsultationErrorCodes.AlreadyCompleted => StatusCodes.Status409Conflict,
            OpdConsultationErrorCodes.NotCompleted => StatusCodes.Status409Conflict,
            OpdConsultationErrorCodes.NotDraft => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest,
        };

        var error = new ApiErrorResponse { ErrorCode = errorCode, Message = message, CorrelationId = HttpContext.GetCorrelationId(), Timestamp = DateTime.UtcNow };
        return StatusCode(status, error);
    }

    private IActionResult MapStructureNoteFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            OpdConsultationErrorCodes.NotFound => StatusCodes.Status404NotFound,
            OpdConsultationErrorCodes.NotDraft => StatusCodes.Status409Conflict,
            OpdConsultationErrorCodes.AiNotConfigured => StatusCodes.Status503ServiceUnavailable,
            OpdConsultationErrorCodes.AiRequestFailed => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status400BadRequest,
        };

        var error = new ApiErrorResponse { ErrorCode = errorCode, Message = message, CorrelationId = HttpContext.GetCorrelationId(), Timestamp = DateTime.UtcNow };
        return StatusCode(status, error);
    }

    private ApiErrorResponse BuildValidationError(ValidationResult validation) => new()
    {
        ErrorCode = "VALIDATION.FAILED",
        Message = "One or more validation errors occurred.",
        ValidationErrors = validation.Errors.Select(e => new ValidationErrorItem { Field = e.PropertyName, Message = e.ErrorMessage }).ToList(),
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };

    private ApiErrorResponse BuildRequestRequiredError() => new()
    {
        ErrorCode = "VALIDATION.FAILED",
        Message = "The request body is missing or could not be parsed.",
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };
}
