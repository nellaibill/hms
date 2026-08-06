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
/// Weekly Roster CRUD, per the Shift Management Phase 3 spec. Roster-header only — it does
/// not generate or reference ShiftAssignments. "Actor" (created/updated/deleted-by) is
/// read from the caller's JWT via ClaimsPrincipalExtensions.GetUserId — matches ShiftsController.
/// </summary>
[ApiController]
[Route("api/v1/weekly-rosters")]
public class WeeklyRostersController : ControllerBase
{
    private readonly IWeeklyRosterService _service;
    private readonly IValidator<CreateWeeklyRosterRequest> _createValidator;
    private readonly IValidator<UpdateWeeklyRosterRequest> _updateValidator;
    private readonly IValidator<CopyWeeklyRosterRequest> _copyValidator;
    private readonly IValidator<MonthlyWeeklyRosterQuery> _monthlyValidator;

    public WeeklyRostersController(
        IWeeklyRosterService service,
        IValidator<CreateWeeklyRosterRequest> createValidator,
        IValidator<UpdateWeeklyRosterRequest> updateValidator,
        IValidator<CopyWeeklyRosterRequest> copyValidator,
        IValidator<MonthlyWeeklyRosterQuery> monthlyValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _copyValidator = copyValidator;
        _monthlyValidator = monthlyValidator;
    }

    /// <summary>Creates a new weekly roster.</summary>
    /// <response code="201">The weekly roster was created.</response>
    /// <response code="400">The request failed validation.</response>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWeeklyRosterRequest request, CancellationToken cancellationToken)
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

    /// <summary>Lists weekly rosters with paging and sorting.</summary>
    /// <response code="200">A page of weekly rosters.</response>
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] WeeklyRosterListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<WeeklyRosterResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Lists weekly rosters whose WeekStartDate falls within the given calendar
    /// month — a read-only view over this same aggregate, not a separate resource.</summary>
    /// <response code="200">A page of weekly rosters for the given month.</response>
    /// <response code="400">Year/Month failed validation.</response>
    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthly([FromQuery] MonthlyWeeklyRosterQuery query, CancellationToken cancellationToken)
    {
        var validation = await _monthlyValidator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var paged = await _service.GetForMonthAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<WeeklyRosterResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Gets a single weekly roster by id.</summary>
    /// <response code="200">The weekly roster was found.</response>
    /// <response code="404">No weekly roster was found for the given id.</response>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Updates a weekly roster.</summary>
    /// <response code="200">The weekly roster was updated.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No weekly roster was found for the given id.</response>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWeeklyRosterRequest request, CancellationToken cancellationToken)
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

    /// <summary>Soft-deletes a weekly roster.</summary>
    /// <response code="204">The weekly roster was deleted.</response>
    /// <response code="404">No weekly roster was found for the given id.</response>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(id, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Publishes a weekly roster. Idempotent — publishing an already-published
    /// roster is a no-op, not an error.</summary>
    /// <response code="200">The weekly roster is published (whether it just became so, or already was).</response>
    /// <response code="404">No weekly roster was found for the given id.</response>
    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.PublishAsync(id, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Duplicates a weekly roster's metadata (DepartmentId) onto a new, unpublished
    /// roster for the caller-chosen target week. Does not copy ShiftAssignments.</summary>
    /// <response code="201">The new weekly roster was created.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No source weekly roster was found for the given id.</response>
    [HttpPost("{id:guid}/copy")]
    public async Task<IActionResult> Copy(Guid id, [FromBody] CopyWeeklyRosterRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(BuildRequestRequiredError());
        }

        var validation = await _copyValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.CopyAsync(id, request, actorId: User.GetUserId(), cancellationToken);
        return !result.IsSuccess
            ? MapFailure(result.ErrorCode!, result.Error!)
            : CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, Envelope(result.Value));
    }

    private static ApiResponse<WeeklyRosterResponse> Envelope(WeeklyRosterResponse? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            HRErrorCodes.NotFound => StatusCodes.Status404NotFound,
            HRErrorCodes.InvalidDepartment => StatusCodes.Status400BadRequest,
            HRErrorCodes.DuplicateRoster => StatusCodes.Status400BadRequest,
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
