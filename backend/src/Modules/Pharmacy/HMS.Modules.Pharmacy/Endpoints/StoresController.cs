using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Pharmacy.Endpoints;

/// <summary>The places the pharmacy holds stock — main store, dispensing counters,
/// sub-stores. No DELETE: a store with stock history is deactivated, never removed.</summary>
[ApiController]
[RequireFeature("pharmacy")]
[Route("api/v1/pharmacy/stores")]
public class StoresController : ControllerBase
{
    private readonly IStoreService _service;
    private readonly IValidator<CreateStoreRequest> _createValidator;
    private readonly IValidator<UpdateStoreRequest> _updateValidator;

    public StoresController(IStoreService service, IValidator<CreateStoreRequest> createValidator, IValidator<UpdateStoreRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Lists every store, the default first. Unpaged — a hospital has a handful.</summary>
    /// <response code="200">The stores.</response>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var stores = await _service.GetAllAsync(cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<StoreResponse>> { Data = stores });
    }

    /// <summary>Creates a store.</summary>
    /// <response code="201">The store was created.</response>
    /// <response code="400">The request failed validation, or the code is already in use.</response>
    [Authorize]
    [RequirePermission("pharmacy.create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStoreRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.CreateAsync(request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, new ApiResponse<StoreResponse> { Data = result.Value })
            : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Renames a store, changes its type, activates or deactivates it, or makes it the default.</summary>
    /// <response code="200">The updated store.</response>
    /// <response code="404">No store with that id exists.</response>
    /// <response code="409">The default store cannot be deactivated.</response>
    [Authorize]
    [RequirePermission("pharmacy.edit")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStoreRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.UpdateAsync(id, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<StoreResponse> { Data = result.Value })
            : MapFailure(result.ErrorCode!, result.Error!);
    }

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            PharmacyErrorCodes.NotFound => StatusCodes.Status404NotFound,
            PharmacyErrorCodes.DefaultStoreRequired => StatusCodes.Status409Conflict,
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
}
