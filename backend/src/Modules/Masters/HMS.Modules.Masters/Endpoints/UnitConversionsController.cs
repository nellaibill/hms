using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Masters.Endpoints;

/// <summary>Unit conversion master CRUD (no hard delete — see docs/03_Masters_ERD's is_active note; deactivate via PUT instead).</summary>
[ApiController]
[Route("api/v1/masters/unit-conversions")]
public class UnitConversionsController : ControllerBase
{
    private readonly IUnitConversionService _service;
    private readonly IValidator<CreateUnitConversionRequest> _createValidator;
    private readonly IValidator<UpdateUnitConversionRequest> _updateValidator;

    public UnitConversionsController(IUnitConversionService service, IValidator<CreateUnitConversionRequest> createValidator, IValidator<UpdateUnitConversionRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Creates a new unit conversion.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUnitConversionRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.CreateAsync(request, actorId: User.GetUserId(), cancellationToken);
        return !result.IsSuccess
            ? MapFailure(result.ErrorCode!, result.Error!)
            : CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, Envelope(result.Value));
    }

    /// <summary>Updates a unit conversion's factor/status (From/To units are immutable — create a new conversion instead).</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUnitConversionRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.UpdateAsync(id, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Gets a single unit conversion by id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Lists unit conversions with paging and active-status filtering.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] UnitConversionListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<UnitConversionResponse>> { Data = paged.Items, Meta = meta });
    }

    private static ApiResponse<UnitConversionResponse> Envelope(UnitConversionResponse? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            MastersErrorCodes.NotFound => StatusCodes.Status404NotFound,
            MastersErrorCodes.DuplicateCode => StatusCodes.Status400BadRequest,
            MastersErrorCodes.InvalidReference => StatusCodes.Status400BadRequest,
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
