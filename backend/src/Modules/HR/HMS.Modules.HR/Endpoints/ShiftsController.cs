using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.HR.Application;
using HMS.Modules.HR.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.HR.Endpoints;

/// <summary>
/// Shift master CRUD, per the Shift Management Phase 1 spec. This module has no
/// authentication yet, so "actor" (created/updated/deleted-by) is null for now — matches
/// UsersController/WarehousesController.
/// </summary>
[ApiController]
[Route("api/v1/shifts")]
public class ShiftsController : ControllerBase
{
    private readonly IShiftService _service;
    private readonly IValidator<CreateShiftRequest> _createValidator;
    private readonly IValidator<UpdateShiftRequest> _updateValidator;

    public ShiftsController(
        IShiftService service,
        IValidator<CreateShiftRequest> createValidator,
        IValidator<UpdateShiftRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Creates a new shift.</summary>
    /// <response code="201">The shift was created.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="409">A shift with the given code already exists.</response>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateShiftRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(BuildRequestRequiredError());
        }

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.CreateAsync(request, actorId: null, cancellationToken);
        return !result.IsSuccess
            ? MapFailure(result.ErrorCode!, result.Error!)
            : CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, Envelope(result.Value));
    }

    /// <summary>Lists shifts with paging, search, sorting, and active-status filtering.</summary>
    /// <response code="200">A page of shifts.</response>
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] ShiftListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<ShiftResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Gets a single shift by id.</summary>
    /// <response code="200">The shift was found.</response>
    /// <response code="404">No shift was found for the given id.</response>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Updates a shift.</summary>
    /// <response code="200">The shift was updated.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No shift was found for the given id.</response>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateShiftRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(BuildRequestRequiredError());
        }

        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.UpdateAsync(id, request, actorId: null, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Soft-deletes a shift.</summary>
    /// <response code="204">The shift was deleted.</response>
    /// <response code="404">No shift was found for the given id.</response>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(id, actorId: null, cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<ShiftResponse> Envelope(ShiftResponse? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            HRErrorCodes.NotFound => StatusCodes.Status404NotFound,
            HRErrorCodes.DuplicateCode => StatusCodes.Status400BadRequest,
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

    // A body that fails to deserialize (e.g. an enum field holding a value that isn't a
    // real member) binds to a null request instead of tripping [ApiController]'s automatic
    // 400 — passing that null straight into FluentValidation throws ArgumentNullException,
    // which surfaces as a raw 500. Guard explicitly instead.
    private ApiErrorResponse BuildRequestRequiredError() => new()
    {
        ErrorCode = "VALIDATION.FAILED",
        Message = "The request body is missing or could not be parsed.",
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };
}
