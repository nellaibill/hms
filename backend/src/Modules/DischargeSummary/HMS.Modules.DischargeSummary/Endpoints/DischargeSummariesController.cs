using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.DischargeSummary.Application;
using HMS.Modules.DischargeSummary.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.DischargeSummary.Endpoints;

/// <summary>
/// Base route is api/v1/discharge-summaries, but Create/GetByAdmission deliberately live
/// under api/v1/admissions/{admissionId}/discharge-summary instead (absolute route
/// overrides, per the approved plan's API section) — a discharge summary is always reached
/// starting from a specific admission for those two actions.
/// </summary>
[ApiController]
[RequireFeature("discharge-summary")]
[Route("api/v1/discharge-summaries")]
public class DischargeSummariesController : ControllerBase
{
    private readonly IDischargeSummaryService _service;
    private readonly IDischargeSummaryAiDraftService _aiDraftService;
    private readonly IValidator<UpdateDischargeSummaryRequest> _updateValidator;
    private readonly IValidator<FinalizeDischargeSummaryRequest> _finalizeValidator;

    public DischargeSummariesController(
        IDischargeSummaryService service,
        IDischargeSummaryAiDraftService aiDraftService,
        IValidator<UpdateDischargeSummaryRequest> updateValidator,
        IValidator<FinalizeDischargeSummaryRequest> finalizeValidator)
    {
        _service = service;
        _aiDraftService = aiDraftService;
        _updateValidator = updateValidator;
        _finalizeValidator = finalizeValidator;
    }

    [Authorize]
    [RequirePermission("discharge-summary.create")]
    [HttpPost("/api/v1/admissions/{admissionId:guid}/discharge-summary")]
    public async Task<IActionResult> Create(Guid admissionId, CancellationToken cancellationToken)
    {
        var result = await _service.CreateDraftAsync(admissionId, actorId: User.GetUserId(), cancellationToken);
        return !result.IsSuccess
            ? MapFailure(result.ErrorCode!, result.Error!)
            : CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, Envelope(result.Value));
    }

    [Authorize]
    [RequirePermission("discharge-summary.view")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    [Authorize]
    [RequirePermission("discharge-summary.view")]
    [HttpGet("/api/v1/admissions/{admissionId:guid}/discharge-summary")]
    public async Task<IActionResult> GetByAdmissionId(Guid admissionId, CancellationToken cancellationToken)
    {
        var result = await _service.GetByAdmissionIdAsync(admissionId, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    [Authorize]
    [RequirePermission("discharge-summary.edit")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDischargeSummaryRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.UpdateAsync(id, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    [Authorize]
    [RequirePermission("discharge-summary.finalize")]
    [HttpPost("{id:guid}/finalize")]
    public async Task<IActionResult> Finalize(Guid id, [FromBody] FinalizeDischargeSummaryRequest? request, CancellationToken cancellationToken)
    {
        request ??= new FinalizeDischargeSummaryRequest();

        var validation = await _finalizeValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.FinalizeAsync(id, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Drafts narrative fields (and vitals from the last IPD reading) from the admission's
    /// IPD data via the configured AI provider. The result is a suggestion only — nothing is saved;
    /// the client merges it into the form and still submits through Update.</summary>
    /// <response code="200">The drafted suggestion.</response>
    /// <response code="403">The summary has already been finalized.</response>
    /// <response code="404">No such discharge summary exists.</response>
    /// <response code="502">The AI provider call failed.</response>
    /// <response code="503">AI drafting isn't configured for this environment.</response>
    [Authorize]
    [RequirePermission("discharge-summary.edit")]
    [RequireFeature("discharge-summary-ai-draft")]
    [HttpPost("{id:guid}/ai/draft")]
    public async Task<IActionResult> AiDraft(Guid id, CancellationToken cancellationToken)
    {
        var result = await _aiDraftService.DraftAsync(id, cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<DischargeSummaryDraftSuggestion> { Data = result.Value })
            : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<DischargeSummaryResponse> Envelope(DischargeSummaryResponse? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            DischargeSummaryErrorCodes.NotFound => StatusCodes.Status404NotFound,
            DischargeSummaryErrorCodes.AlreadyExists => StatusCodes.Status409Conflict,
            DischargeSummaryErrorCodes.AlreadyFinalized => StatusCodes.Status409Conflict,
            DischargeSummaryErrorCodes.NotDraft => StatusCodes.Status403Forbidden,
            DischargeSummaryErrorCodes.AiNotConfigured => StatusCodes.Status503ServiceUnavailable,
            DischargeSummaryErrorCodes.AiRequestFailed => StatusCodes.Status502BadGateway,
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
