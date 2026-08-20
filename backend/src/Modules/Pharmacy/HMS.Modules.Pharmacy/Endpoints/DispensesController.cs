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

/// <summary>Direct-dispense stock issues to a patient — decreasing a product/batch's on-hand
/// balance. No PUT/DELETE: the stock ledger is append-only.</summary>
[ApiController]
[RequireFeature("pharmacy")]
[Route("api/v1/pharmacy/dispenses")]
public class DispensesController : ControllerBase
{
    private readonly IDispenseService _service;
    private readonly IValidator<CreateDispenseRequest> _createValidator;

    public DispensesController(
        IDispenseService service,
        IValidator<CreateDispenseRequest> createValidator)
    {
        _service = service;
        _createValidator = createValidator;
    }

    /// <summary>Dispenses stock from a product/batch to a patient, decreasing its on-hand balance.</summary>
    /// <response code="201">The dispense was recorded.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="409">The product/batch/patient reference is invalid, the batch has expired, or there is not enough stock on hand.</response>
    [Authorize]
    [RequirePermission("pharmacy.create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDispenseRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.CreateAsync(request, actorId: User.GetUserId(), cancellationToken);
        return !result.IsSuccess
            ? MapFailure(result.ErrorCode!, result.Error!)
            : CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, Envelope(result.Value));
    }

    /// <summary>Lists dispenses, newest first, optionally filtered by patient and/or product.</summary>
    /// <response code="200">The page of dispenses.</response>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] DispenseListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };
        return Ok(new ApiResponse<IReadOnlyList<DispenseResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Gets a single dispense by id.</summary>
    /// <response code="200">The dispense.</response>
    /// <response code="404">No dispense with that id exists.</response>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<DispenseResponse> Envelope(DispenseResponse? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            PharmacyErrorCodes.NotFound => StatusCodes.Status404NotFound,
            PharmacyErrorCodes.InvalidProduct => StatusCodes.Status409Conflict,
            PharmacyErrorCodes.InvalidBatch => StatusCodes.Status409Conflict,
            PharmacyErrorCodes.InvalidPatient => StatusCodes.Status409Conflict,
            PharmacyErrorCodes.BatchExpired => StatusCodes.Status409Conflict,
            PharmacyErrorCodes.InsufficientStock => StatusCodes.Status409Conflict,
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
