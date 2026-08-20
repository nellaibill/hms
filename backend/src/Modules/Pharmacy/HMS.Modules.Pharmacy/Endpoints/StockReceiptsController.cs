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

/// <summary>Direct-dispense stock receipts — recording new stock received against a
/// product/batch. No PUT/DELETE: the stock ledger is append-only.</summary>
[ApiController]
[RequireFeature("pharmacy")]
[Route("api/v1/pharmacy/stock-receipts")]
public class StockReceiptsController : ControllerBase
{
    private readonly IStockReceiptService _service;
    private readonly IValidator<CreateStockReceiptRequest> _createValidator;

    public StockReceiptsController(
        IStockReceiptService service,
        IValidator<CreateStockReceiptRequest> createValidator)
    {
        _service = service;
        _createValidator = createValidator;
    }

    /// <summary>Records a new stock receipt against a product/batch, increasing its on-hand balance.</summary>
    /// <response code="201">The stock receipt was recorded.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="409">The product or batch reference is invalid.</response>
    [Authorize]
    [RequirePermission("pharmacy.create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStockReceiptRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.CreateAsync(request, actorId: User.GetUserId(), cancellationToken);
        return !result.IsSuccess
            ? MapFailure(result.ErrorCode!, result.Error!)
            : CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, Envelope(result.Value));
    }

    /// <summary>Lists stock receipts, newest first, optionally filtered by product.</summary>
    /// <response code="200">The page of stock receipts.</response>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] StockReceiptListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };
        return Ok(new ApiResponse<IReadOnlyList<StockReceiptResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Gets a single stock receipt by id.</summary>
    /// <response code="200">The stock receipt.</response>
    /// <response code="404">No stock receipt with that id exists.</response>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<StockReceiptResponse> Envelope(StockReceiptResponse? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            PharmacyErrorCodes.NotFound => StatusCodes.Status404NotFound,
            PharmacyErrorCodes.InvalidProduct => StatusCodes.Status409Conflict,
            PharmacyErrorCodes.InvalidBatch => StatusCodes.Status409Conflict,
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
