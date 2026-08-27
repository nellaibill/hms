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
/// Employee leave request workflow (submit, approve, reject, cancel) for the Hospital HR
/// Management MVP — see docs/DecisionLog.md ADR-036.
/// </summary>
[ApiController]
[RequireFeature("hr")]
[Route("api/v1/leave-requests")]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _service;
    private readonly IValidator<CreateLeaveRequestRequest> _createValidator;
    private readonly IValidator<ApproveLeaveRequestRequest> _approveValidator;
    private readonly IValidator<RejectLeaveRequestRequest> _rejectValidator;

    public LeaveRequestsController(
        ILeaveRequestService service,
        IValidator<CreateLeaveRequestRequest> createValidator,
        IValidator<ApproveLeaveRequestRequest> approveValidator,
        IValidator<RejectLeaveRequestRequest> rejectValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _approveValidator = approveValidator;
        _rejectValidator = rejectValidator;
    }

    /// <summary>Submits a new leave request (starts as Pending).</summary>
    /// <response code="201">The leave request was created.</response>
    /// <response code="400">The request failed validation, or the employee/leave type is invalid.</response>
    [Authorize]
    [RequirePermission("workforce-admin.create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeaveRequestRequest request, CancellationToken cancellationToken)
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

    /// <summary>Lists leave requests with paging, filtering by employee/status/leave type/date-range.</summary>
    /// <response code="200">A page of leave requests.</response>
    [Authorize]
    [RequirePermission("workforce-admin.view")]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] LeaveRequestListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<LeaveRequestResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Gets a single leave request by id.</summary>
    /// <response code="200">The leave request was found.</response>
    /// <response code="404">No leave request was found for the given id.</response>
    [Authorize]
    [RequirePermission("workforce-admin.view")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Approves a Pending leave request.</summary>
    /// <response code="200">The leave request was approved.</response>
    /// <response code="400">The leave request is not Pending.</response>
    /// <response code="404">No leave request was found for the given id.</response>
    [Authorize]
    [RequirePermission("workforce-admin.edit")]
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveLeaveRequestRequest? request, CancellationToken cancellationToken)
    {
        request ??= new ApproveLeaveRequestRequest();

        var validation = await _approveValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.ApproveAsync(id, request, actorUserId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Rejects a Pending leave request — a reason is required.</summary>
    /// <response code="200">The leave request was rejected.</response>
    /// <response code="400">The request failed validation (a reason is required), or the leave request is not Pending.</response>
    /// <response code="404">No leave request was found for the given id.</response>
    [Authorize]
    [RequirePermission("workforce-admin.edit")]
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectLeaveRequestRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(BuildRequestRequiredError());
        }

        var validation = await _rejectValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.RejectAsync(id, request, actorUserId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Cancels a Pending leave request.</summary>
    /// <response code="200">The leave request was cancelled.</response>
    /// <response code="400">The leave request is not Pending.</response>
    /// <response code="404">No leave request was found for the given id.</response>
    [Authorize]
    [RequirePermission("workforce-admin.edit")]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.CancelAsync(id, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<LeaveRequestResponse> Envelope(LeaveRequestResponse? data) => new() { Data = data };

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
