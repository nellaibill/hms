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

[ApiController]
[RequireFeature("ipd")]
[Route("api/v1/ipd/admissions/{admissionId:guid}/medication-orders")]
public class MedicationOrdersController : ControllerBase
{
    private readonly IMedicationOrderService _service;
    private readonly IValidator<CreateMedicationOrderRequest> _createValidator;
    private readonly IValidator<DiscontinueMedicationOrderRequest> _discontinueValidator;

    public MedicationOrdersController(
        IMedicationOrderService service,
        IValidator<CreateMedicationOrderRequest> createValidator,
        IValidator<DiscontinueMedicationOrderRequest> discontinueValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _discontinueValidator = discontinueValidator;
    }

    [Authorize]
    [RequirePermission("clinical-care.create")]
    [HttpPost]
    public async Task<IActionResult> Create(Guid admissionId, [FromBody] CreateMedicationOrderRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.CreateAsync(admissionId, request, actorId: User.GetUserId(), cancellationToken);
        return !result.IsSuccess
            ? MapFailure(result.ErrorCode!, result.Error!)
            : StatusCode(StatusCodes.Status201Created, new ApiResponse<MedicationOrderResponse> { Data = result.Value });
    }

    [Authorize]
    [RequirePermission("clinical-care.view")]
    [HttpGet]
    public async Task<IActionResult> GetByAdmissionId(Guid admissionId, CancellationToken cancellationToken)
    {
        var result = await _service.GetByAdmissionIdAsync(admissionId, cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<IReadOnlyList<MedicationOrderResponse>> { Data = result.Value })
            : MapFailure(result.ErrorCode!, result.Error!);
    }

    [Authorize]
    [RequirePermission("clinical-care.edit")]
    [HttpPost("{orderId:guid}/discontinue")]
    public async Task<IActionResult> Discontinue(Guid admissionId, Guid orderId, [FromBody] DiscontinueMedicationOrderRequest? request, CancellationToken cancellationToken)
    {
        request ??= new DiscontinueMedicationOrderRequest();

        var validation = await _discontinueValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.DiscontinueAsync(admissionId, orderId, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(new ApiResponse<MedicationOrderResponse> { Data = result.Value }) : MapFailure(result.ErrorCode!, result.Error!);
    }

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            IPDErrorCodes.NotFound => StatusCodes.Status404NotFound,
            IPDErrorCodes.MedicationOrderNotFound => StatusCodes.Status404NotFound,
            IPDErrorCodes.MedicationOrderAlreadyDiscontinued => StatusCodes.Status409Conflict,
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
