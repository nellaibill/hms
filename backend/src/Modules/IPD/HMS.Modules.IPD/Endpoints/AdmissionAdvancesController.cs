using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.IPD.Endpoints;

/// <summary>
/// Records money collected from the patient's family as an advance/deposit during the stay.
/// Gated by "finance-billing.*" (Billing's own permission), not "clinical-care.*" — this
/// records real money received, same reasoning as IPDBillingController. See
/// Application/AdmissionAdvanceService.cs and ADR-067.
/// </summary>
[ApiController]
[RequireFeature("ipd")]
[Route("api/v1/ipd/admissions/{admissionId:guid}/advances")]
public class AdmissionAdvancesController : ControllerBase
{
    private readonly IAdmissionAdvanceService _service;
    private readonly IValidator<CreateAdmissionAdvanceRequest> _createValidator;

    public AdmissionAdvancesController(IAdmissionAdvanceService service, IValidator<CreateAdmissionAdvanceRequest> createValidator)
    {
        _service = service;
        _createValidator = createValidator;
    }

    [Authorize]
    [RequirePermission("finance-billing.create")]
    [HttpPost]
    public async Task<IActionResult> Create(Guid admissionId, [FromBody] CreateAdmissionAdvanceRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.CreateAsync(admissionId, request, actorId: User.GetUserId(), cancellationToken);
        return !result.IsSuccess
            ? MapFailure(result.ErrorCode!, result.Error!)
            : StatusCode(StatusCodes.Status201Created, new ApiResponse<AdmissionAdvanceResponse> { Data = result.Value });
    }

    [Authorize]
    [RequirePermission("finance-billing.view")]
    [HttpGet]
    public async Task<IActionResult> GetByAdmissionId(Guid admissionId, CancellationToken cancellationToken)
    {
        var result = await _service.GetByAdmissionIdAsync(admissionId, cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<IReadOnlyList<AdmissionAdvanceResponse>> { Data = result.Value })
            : MapFailure(result.ErrorCode!, result.Error!);
    }

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            IPDErrorCodes.NotFound => StatusCodes.Status404NotFound,
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
