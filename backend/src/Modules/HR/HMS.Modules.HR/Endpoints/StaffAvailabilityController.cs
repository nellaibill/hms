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
/// Staff Availability CRUD, per the Shift Management Phase 4 spec. Purely informational —
/// nothing here reads or enforces this data against ShiftAssignments yet. "Actor"
/// (created/updated/deleted-by) is read from the caller's JWT via
/// ClaimsPrincipalExtensions.GetUserId — matches ShiftsController. Singular route
/// (staff-availability, not -availabilities) per spec.
/// </summary>
[ApiController]
[Route("api/v1/staff-availability")]
public class StaffAvailabilityController : ControllerBase
{
    private readonly IStaffAvailabilityService _service;
    private readonly IValidator<CreateStaffAvailabilityRequest> _createValidator;
    private readonly IValidator<UpdateStaffAvailabilityRequest> _updateValidator;

    public StaffAvailabilityController(
        IStaffAvailabilityService service,
        IValidator<CreateStaffAvailabilityRequest> createValidator,
        IValidator<UpdateStaffAvailabilityRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Creates a new staff availability record.</summary>
    /// <response code="201">The staff availability record was created.</response>
    /// <response code="400">The request failed validation.</response>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStaffAvailabilityRequest request, CancellationToken cancellationToken)
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

    /// <summary>Lists staff availability records with paging, search (Reason), and sorting.</summary>
    /// <response code="200">A page of staff availability records.</response>
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] StaffAvailabilityListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<StaffAvailabilityResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Gets a single staff availability record by id.</summary>
    /// <response code="200">The staff availability record was found.</response>
    /// <response code="404">No staff availability record was found for the given id.</response>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Updates a staff availability record.</summary>
    /// <response code="200">The staff availability record was updated.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No staff availability record was found for the given id.</response>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStaffAvailabilityRequest request, CancellationToken cancellationToken)
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

    /// <summary>Soft-deletes a staff availability record.</summary>
    /// <response code="204">The staff availability record was deleted.</response>
    /// <response code="404">No staff availability record was found for the given id.</response>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(id, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<StaffAvailabilityResponse> Envelope(StaffAvailabilityResponse? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            HRErrorCodes.NotFound => StatusCodes.Status404NotFound,
            HRErrorCodes.InvalidStaff => StatusCodes.Status400BadRequest,
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
