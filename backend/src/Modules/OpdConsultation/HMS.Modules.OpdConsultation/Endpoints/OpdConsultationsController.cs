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

    public OpdConsultationsController(
        IOpdConsultationService service,
        IValidator<SaveOpdConsultationRequest> saveValidator,
        IValidator<StructureConsultationNoteRequest> structureNoteValidator)
    {
        _service = service;
        _saveValidator = saveValidator;
        _structureNoteValidator = structureNoteValidator;
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
