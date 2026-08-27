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
/// Daily attendance tracking (check-in/check-out plus manual corrections) for the Hospital HR
/// Management MVP — see docs/DecisionLog.md ADR-036.
/// </summary>
[ApiController]
[RequireFeature("hr")]
[Route("api/v1/attendance")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _service;
    private readonly IValidator<CreateAttendanceRequest> _createValidator;
    private readonly IValidator<UpdateAttendanceRequest> _updateValidator;
    private readonly IValidator<CheckInRequest> _checkInValidator;
    private readonly IValidator<CheckOutRequest> _checkOutValidator;

    public AttendanceController(
        IAttendanceService service,
        IValidator<CreateAttendanceRequest> createValidator,
        IValidator<UpdateAttendanceRequest> updateValidator,
        IValidator<CheckInRequest> checkInValidator,
        IValidator<CheckOutRequest> checkOutValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _checkInValidator = checkInValidator;
        _checkOutValidator = checkOutValidator;
    }

    /// <summary>Creates an attendance record directly (manual correction — e.g. marking Absent/OnLeave without a check-in).</summary>
    /// <response code="201">The attendance record was created.</response>
    /// <response code="400">The request failed validation, the employee is invalid, or a record already exists for that employee/date.</response>
    [Authorize]
    [RequirePermission("workforce-admin.create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAttendanceRequest request, CancellationToken cancellationToken)
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

    /// <summary>Lists attendance records with paging, date-range/employee/department/status filtering.</summary>
    /// <response code="200">A page of attendance records.</response>
    [Authorize]
    [RequirePermission("workforce-admin.view")]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] AttendanceListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<AttendanceResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Gets a single attendance record by id.</summary>
    /// <response code="200">The attendance record was found.</response>
    /// <response code="404">No attendance record was found for the given id.</response>
    [Authorize]
    [RequirePermission("workforce-admin.view")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Updates an attendance record directly (manual correction).</summary>
    /// <response code="200">The attendance record was updated.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No attendance record was found for the given id.</response>
    [Authorize]
    [RequirePermission("workforce-admin.edit")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAttendanceRequest request, CancellationToken cancellationToken)
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

    /// <summary>Checks an employee in for today — creates or updates today's row, defaults CheckInTime to now, sets Status to Present on a fresh row.</summary>
    /// <response code="200">The check-in was recorded.</response>
    /// <response code="400">The employee is invalid, or they've already checked in today.</response>
    [Authorize]
    [RequirePermission("workforce-admin.create")]
    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(BuildRequestRequiredError());
        }

        var validation = await _checkInValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.CheckInAsync(request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Checks an employee out for today.</summary>
    /// <response code="200">The check-out was recorded.</response>
    /// <response code="400">The employee is invalid, hasn't checked in yet, or has already checked out today.</response>
    [Authorize]
    [RequirePermission("workforce-admin.edit")]
    [HttpPost("check-out")]
    public async Task<IActionResult> CheckOut([FromBody] CheckOutRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(BuildRequestRequiredError());
        }

        var validation = await _checkOutValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.CheckOutAsync(request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<AttendanceResponse> Envelope(AttendanceResponse? data) => new() { Data = data };

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
