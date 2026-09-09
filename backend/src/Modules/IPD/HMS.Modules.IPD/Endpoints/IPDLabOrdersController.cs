using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.Laboratory.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.IPD.Endpoints;

/// <summary>
/// Places/lists real HMS.Modules.Laboratory LabOrders directly against an admission — no
/// invoice required. See Application/IPDLabOrderService.cs and ADR-064.
/// </summary>
[ApiController]
[RequireFeature("ipd")]
[Route("api/v1/ipd/admissions/{admissionId:guid}/lab-orders")]
public class IPDLabOrdersController : ControllerBase
{
    private readonly IIPDLabOrderService _service;
    private readonly IValidator<CreatePlaceLabOrderRequest> _createValidator;

    public IPDLabOrdersController(IIPDLabOrderService service, IValidator<CreatePlaceLabOrderRequest> createValidator)
    {
        _service = service;
        _createValidator = createValidator;
    }

    [Authorize]
    [RequirePermission("clinical-care.create")]
    [HttpPost]
    public async Task<IActionResult> Create(Guid admissionId, [FromBody] CreatePlaceLabOrderRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.PlaceOrderAsync(admissionId, request, actorId: User.GetUserId(), cancellationToken);
        return !result.IsSuccess
            ? MapFailure(result.ErrorCode!, result.Error!)
            : StatusCode(StatusCodes.Status201Created, new ApiResponse<LabOrderResponse> { Data = result.Value });
    }

    [Authorize]
    [RequirePermission("clinical-care.view")]
    [HttpGet]
    public async Task<IActionResult> GetByAdmissionId(Guid admissionId, CancellationToken cancellationToken)
    {
        var result = await _service.GetByAdmissionIdAsync(admissionId, cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<IReadOnlyList<LabOrderResponse>> { Data = result.Value })
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
