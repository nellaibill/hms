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
[Route("api/v1/ipd/admissions/{admissionId:guid}/doctor-orders")]
public class DoctorOrdersController : ControllerBase
{
    private readonly IDoctorOrderService _service;
    private readonly IValidator<CreateDoctorOrderRequest> _createValidator;
    private readonly IValidator<CancelDoctorOrderRequest> _cancelValidator;

    public DoctorOrdersController(
        IDoctorOrderService service,
        IValidator<CreateDoctorOrderRequest> createValidator,
        IValidator<CancelDoctorOrderRequest> cancelValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _cancelValidator = cancelValidator;
    }

    [Authorize]
    [RequirePermission("clinical-care.create")]
    [HttpPost]
    public async Task<IActionResult> Create(Guid admissionId, [FromBody] CreateDoctorOrderRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.CreateAsync(admissionId, request, actorId: User.GetUserId(), cancellationToken);
        return !result.IsSuccess
            ? MapFailure(result.ErrorCode!, result.Error!)
            : StatusCode(StatusCodes.Status201Created, new ApiResponse<DoctorOrderResponse> { Data = result.Value });
    }

    [Authorize]
    [RequirePermission("clinical-care.view")]
    [HttpGet]
    public async Task<IActionResult> GetByAdmissionId(Guid admissionId, CancellationToken cancellationToken)
    {
        var result = await _service.GetByAdmissionIdAsync(admissionId, cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<IReadOnlyList<DoctorOrderResponse>> { Data = result.Value })
            : MapFailure(result.ErrorCode!, result.Error!);
    }

    [Authorize]
    [RequirePermission("clinical-care.edit")]
    [HttpPost("{orderId:guid}/advance")]
    public async Task<IActionResult> Advance(Guid admissionId, Guid orderId, CancellationToken cancellationToken)
    {
        var result = await _service.AdvanceAsync(admissionId, orderId, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(new ApiResponse<DoctorOrderResponse> { Data = result.Value }) : MapFailure(result.ErrorCode!, result.Error!);
    }

    [Authorize]
    [RequirePermission("clinical-care.edit")]
    [HttpPost("{orderId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid admissionId, Guid orderId, [FromBody] CancelDoctorOrderRequest? request, CancellationToken cancellationToken)
    {
        request ??= new CancelDoctorOrderRequest();

        var validation = await _cancelValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.CancelAsync(admissionId, orderId, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(new ApiResponse<DoctorOrderResponse> { Data = result.Value }) : MapFailure(result.ErrorCode!, result.Error!);
    }

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            IPDErrorCodes.NotFound => StatusCodes.Status404NotFound,
            IPDErrorCodes.DoctorOrderNotFound => StatusCodes.Status404NotFound,
            IPDErrorCodes.InvalidOrderStatusTransition => StatusCodes.Status409Conflict,
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
