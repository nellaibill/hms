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

    public OpdConsultationsController(IOpdConsultationService service, IValidator<SaveOpdConsultationRequest> saveValidator)
    {
        _service = service;
        _saveValidator = saveValidator;
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
            OpdConsultationErrorCodes.NotDraft => StatusCodes.Status403Forbidden,
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
