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
/// Shift Assignment CRUD, per the Shift Management Phase 2 spec. This module has no
/// authentication yet, so "actor" (created/updated/deleted-by) is null for now — matches
/// ShiftsController/UsersController.
/// </summary>
[ApiController]
[Route("api/v1/shift-assignments")]
public class ShiftAssignmentsController : ControllerBase
{
    private readonly IShiftAssignmentService _service;
    private readonly IValidator<CreateShiftAssignmentRequest> _createValidator;
    private readonly IValidator<UpdateShiftAssignmentRequest> _updateValidator;

    public ShiftAssignmentsController(
        IShiftAssignmentService service,
        IValidator<CreateShiftAssignmentRequest> createValidator,
        IValidator<UpdateShiftAssignmentRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Creates a new shift assignment.</summary>
    /// <response code="201">The shift assignment was created.</response>
    /// <response code="400">The request failed validation, or ShiftId does not reference an existing shift.</response>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateShiftAssignmentRequest request, CancellationToken cancellationToken)
    {
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

    /// <summary>Lists shift assignments with paging, search (Remarks), and sorting.</summary>
    /// <response code="200">A page of shift assignments.</response>
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] ShiftAssignmentListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<ShiftAssignmentResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Gets a single shift assignment by id.</summary>
    /// <response code="200">The shift assignment was found.</response>
    /// <response code="404">No shift assignment was found for the given id.</response>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Updates a shift assignment.</summary>
    /// <response code="200">The shift assignment was updated.</response>
    /// <response code="400">The request failed validation, or ShiftId does not reference an existing shift.</response>
    /// <response code="404">No shift assignment was found for the given id.</response>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateShiftAssignmentRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.UpdateAsync(id, request, actorId: null, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Soft-deletes a shift assignment.</summary>
    /// <response code="204">The shift assignment was deleted.</response>
    /// <response code="404">No shift assignment was found for the given id.</response>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(id, actorId: null, cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<ShiftAssignmentResponse> Envelope(ShiftAssignmentResponse? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            HRErrorCodes.NotFound => StatusCodes.Status404NotFound,
            HRErrorCodes.InvalidShift => StatusCodes.Status400BadRequest,
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
