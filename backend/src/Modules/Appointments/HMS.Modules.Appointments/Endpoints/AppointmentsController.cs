using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Appointments.Application;
using HMS.Modules.Appointments.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Appointments.Endpoints;

/// <summary>
/// Appointment booking. Reuses reception's "patient-management.*" permissions (create to book,
/// edit to reschedule, cancel, mark no-show, check in and manage leave blocks). Reading is also
/// open to "clinical-care.view", so consultants can see their own diary.
/// </summary>
[ApiController]
[Authorize]
[RequireFeature("appointments")]
[Route("api/v1/appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _service;
    private readonly IValidator<BookAppointmentRequest> _bookValidator;
    private readonly IValidator<RescheduleAppointmentRequest> _rescheduleValidator;
    private readonly IValidator<CancelAppointmentRequest> _cancelValidator;
    private readonly IValidator<MarkAppointmentArrivedRequest> _arriveValidator;
    private readonly IValidator<AppointmentListQuery> _listValidator;
    private readonly IValidator<AppointmentSlotsQuery> _slotsValidator;
    private readonly IValidator<AppointmentSummaryQuery> _summaryValidator;
    private readonly IValidator<CreateConsultantBlockRequest> _blockValidator;

    public AppointmentsController(
        IAppointmentService service,
        IValidator<BookAppointmentRequest> bookValidator,
        IValidator<RescheduleAppointmentRequest> rescheduleValidator,
        IValidator<CancelAppointmentRequest> cancelValidator,
        IValidator<MarkAppointmentArrivedRequest> arriveValidator,
        IValidator<AppointmentListQuery> listValidator,
        IValidator<AppointmentSlotsQuery> slotsValidator,
        IValidator<AppointmentSummaryQuery> summaryValidator,
        IValidator<CreateConsultantBlockRequest> blockValidator)
    {
        _service = service;
        _bookValidator = bookValidator;
        _rescheduleValidator = rescheduleValidator;
        _cancelValidator = cancelValidator;
        _arriveValidator = arriveValidator;
        _listValidator = listValidator;
        _slotsValidator = slotsValidator;
        _summaryValidator = summaryValidator;
        _blockValidator = blockValidator;
    }

    private bool CanView => User.HasPermission("patient-management.view") || User.HasPermission("clinical-care.view");

    /// <summary>Lists appointments with date-range, consultant, department, patient and status filters.</summary>
    /// <response code="200">A page of appointments, in date, time and token order by default.</response>
    /// <response code="400">The query failed validation.</response>
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] AppointmentListQuery query, CancellationToken cancellationToken)
    {
        if (!CanView)
        {
            return Forbid();
        }

        var validation = await _listValidator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<AppointmentResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>One consultant's slots for one day, with how many seats are booked in each.</summary>
    /// <response code="200">The day's slots (empty with a message when the consultant has no clinic that day).</response>
    /// <response code="400">The query failed validation, or the consultant doesn't exist.</response>
    [HttpGet("slots")]
    public async Task<IActionResult> GetSlots([FromQuery] AppointmentSlotsQuery query, CancellationToken cancellationToken)
    {
        if (!CanView)
        {
            return Forbid();
        }

        var validation = await _slotsValidator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.GetSlotsAsync(query, cancellationToken);
        return result.IsSuccess ? Ok(new ApiResponse<AppointmentSlotsResponse> { Data = result.Value }) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Counts of a day's appointments by status.</summary>
    /// <response code="200">The day's counts.</response>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] AppointmentSummaryQuery query, CancellationToken cancellationToken)
    {
        if (!CanView)
        {
            return Forbid();
        }

        var validation = await _summaryValidator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        return Ok(new ApiResponse<AppointmentSummaryResponse> { Data = await _service.GetSummaryAsync(query, cancellationToken) });
    }

    /// <summary>Gets one appointment.</summary>
    /// <response code="200">The appointment.</response>
    /// <response code="404">No appointment has this id.</response>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!CanView)
        {
            return Forbid();
        }

        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Books an appointment into one of a consultant's slots.</summary>
    /// <response code="201">The appointment was booked.</response>
    /// <response code="400">Validation failed, or the slot can't be booked (past, closed, not a slot, or a duplicate).</response>
    /// <response code="409">The slot is full.</response>
    [RequirePermission("patient-management.create")]
    [HttpPost]
    public async Task<IActionResult> Book([FromBody] BookAppointmentRequest request, CancellationToken cancellationToken)
    {
        var validation = await _bookValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.BookAsync(request, User.GetUserId(), cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, Envelope(result.Value))
            : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Moves a Booked appointment to another slot of the same consultant.</summary>
    /// <response code="200">The appointment was rescheduled.</response>
    /// <response code="400">Validation failed, or the new slot can't be booked.</response>
    /// <response code="404">No appointment has this id.</response>
    /// <response code="409">The new slot is full, or the appointment is no longer Booked.</response>
    [RequirePermission("patient-management.edit")]
    [HttpPost("{id:guid}/reschedule")]
    public async Task<IActionResult> Reschedule(Guid id, [FromBody] RescheduleAppointmentRequest request, CancellationToken cancellationToken)
    {
        var validation = await _rescheduleValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.RescheduleAsync(id, request, User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Cancels a Booked appointment, giving its seat back.</summary>
    /// <response code="200">The appointment was cancelled.</response>
    /// <response code="404">No appointment has this id.</response>
    /// <response code="409">The appointment is no longer Booked.</response>
    [RequirePermission("patient-management.edit")]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelAppointmentRequest request, CancellationToken cancellationToken)
    {
        var validation = await _cancelValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.CancelAsync(id, request, User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Marks a Booked appointment as a no-show (on or after its day).</summary>
    /// <response code="200">The appointment was marked as a no-show.</response>
    /// <response code="400">The appointment's day hasn't come yet.</response>
    /// <response code="404">No appointment has this id.</response>
    /// <response code="409">The appointment is no longer Booked.</response>
    [RequirePermission("patient-management.edit")]
    [HttpPost("{id:guid}/no-show")]
    public async Task<IActionResult> MarkNoShow(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.MarkNoShowAsync(id, User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Checks a patient in: links the visit reception just recorded to the appointment.</summary>
    /// <response code="200">The appointment is now Arrived.</response>
    /// <response code="400">Validation failed, it isn't the appointment's day, or the patient/visit doesn't match.</response>
    /// <response code="404">No appointment has this id.</response>
    /// <response code="409">The appointment is no longer Booked.</response>
    [RequirePermission("patient-management.edit")]
    [HttpPost("{id:guid}/arrive")]
    public async Task<IActionResult> MarkArrived(Guid id, [FromBody] MarkAppointmentArrivedRequest request, CancellationToken cancellationToken)
    {
        var validation = await _arriveValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.MarkArrivedAsync(id, request, User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Lists consultant leave blocks overlapping a date range.</summary>
    /// <response code="200">The blocks, earliest first (at most 200).</response>
    [HttpGet("blocks")]
    public async Task<IActionResult> GetBlocks([FromQuery] ConsultantBlockListQuery query, CancellationToken cancellationToken)
    {
        if (!CanView)
        {
            return Forbid();
        }

        return Ok(new ApiResponse<IReadOnlyList<ConsultantBlockResponse>> { Data = await _service.GetBlocksAsync(query, cancellationToken) });
    }

    /// <summary>Blocks a consultant's slots (leave, conference, cancelled clinic) for a range of days.</summary>
    /// <response code="201">The block was created; the response lists Booked appointments that fall inside it.</response>
    /// <response code="400">Validation failed, the consultant doesn't exist, or the range starts in the past.</response>
    [RequirePermission("patient-management.edit")]
    [HttpPost("blocks")]
    public async Task<IActionResult> CreateBlock([FromBody] CreateConsultantBlockRequest request, CancellationToken cancellationToken)
    {
        var validation = await _blockValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.CreateBlockAsync(request, User.GetUserId(), cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, new ApiResponse<ConsultantBlockCreatedResponse> { Data = result.Value })
            : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Removes a leave block, opening its slots again.</summary>
    /// <response code="204">The block was removed.</response>
    /// <response code="404">No block has this id.</response>
    [RequirePermission("patient-management.edit")]
    [HttpDelete("blocks/{id:guid}")]
    public async Task<IActionResult> RemoveBlock(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.RemoveBlockAsync(id, User.GetUserId(), cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<AppointmentResponse> Envelope(AppointmentResponse? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            AppointmentErrorCodes.NotFound or AppointmentErrorCodes.BlockNotFound => StatusCodes.Status404NotFound,
            AppointmentErrorCodes.SlotFull or AppointmentErrorCodes.InvalidStatus => StatusCodes.Status409Conflict,
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
