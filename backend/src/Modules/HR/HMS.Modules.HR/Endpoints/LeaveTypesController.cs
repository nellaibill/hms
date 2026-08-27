using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.HR.Application;
using HMS.Modules.HR.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.HR.Endpoints;

/// <summary>
/// Leave type master CRUD — small, HR-specific reference data (not shared by any other
/// module, so it stays inside HR rather than Masters — see docs/DecisionLog.md ADR-036).
/// </summary>
[ApiController]
[RequireFeature("hr")]
[Route("api/v1/leave-types")]
public class LeaveTypesController : ControllerBase
{
    private readonly ILeaveTypeService _service;
    private readonly IValidator<CreateLeaveTypeRequest> _createValidator;
    private readonly IValidator<UpdateLeaveTypeRequest> _updateValidator;

    public LeaveTypesController(
        ILeaveTypeService service,
        IValidator<CreateLeaveTypeRequest> createValidator,
        IValidator<UpdateLeaveTypeRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Creates a new leave type.</summary>
    /// <response code="201">The leave type was created.</response>
    /// <response code="400">The request failed validation, or the code is already in use.</response>
    [Authorize]
    [RequirePermission("workforce-admin.create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeaveTypeRequest request, CancellationToken cancellationToken)
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

        var result = await _service.CreateAsync(request, actorId: User.GetUserId(), cancellationToken);
        return !result.IsSuccess
            ? MapFailure(result.ErrorCode!, result.Error!)
            : CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, Envelope(result.Value));
    }

    /// <summary>Lists leave types with paging, search, sorting, and active-status filtering.</summary>
    /// <response code="200">A page of leave types.</response>
    [Authorize]
    [RequirePermission("workforce-admin.view")]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] LeaveTypeListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<LeaveTypeResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Gets a single leave type by id.</summary>
    /// <response code="200">The leave type was found.</response>
    /// <response code="404">No leave type was found for the given id.</response>
    [Authorize]
    [RequirePermission("workforce-admin.view")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Updates a leave type.</summary>
    /// <response code="200">The leave type was updated.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No leave type was found for the given id.</response>
    [Authorize]
    [RequirePermission("workforce-admin.edit")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeaveTypeRequest request, CancellationToken cancellationToken)
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

        var result = await _service.UpdateAsync(id, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Soft-deletes a leave type.</summary>
    /// <response code="204">The leave type was deleted.</response>
    /// <response code="404">No leave type was found for the given id.</response>
    [Authorize]
    [RequirePermission("workforce-admin.delete")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(id, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<LeaveTypeResponse> Envelope(LeaveTypeResponse? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            HRErrorCodes.NotFound => StatusCodes.Status404NotFound,
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
