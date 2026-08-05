using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Calendar.Application;
using HMS.Modules.Calendar.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Calendar.Endpoints;

/// <summary>
/// Event CRUD + monthly view, per the Calendar Phase 1/Phase 2 spec. This module has no
/// authentication yet, so "actor" (created/updated/deleted-by) is null for now — matches
/// every other module's controllers.
/// </summary>
[ApiController]
[Route("api/v1/events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _service;
    private readonly IValidator<CreateEventRequest> _createValidator;
    private readonly IValidator<UpdateEventRequest> _updateValidator;
    private readonly IValidator<MonthlyEventQuery> _monthlyValidator;
    private readonly IValidator<BulkCreateEventsRequest> _bulkValidator;

    public EventsController(
        IEventService service,
        IValidator<CreateEventRequest> createValidator,
        IValidator<UpdateEventRequest> updateValidator,
        IValidator<MonthlyEventQuery> monthlyValidator,
        IValidator<BulkCreateEventsRequest> bulkValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _monthlyValidator = monthlyValidator;
        _bulkValidator = bulkValidator;
    }

    /// <summary>Creates a new calendar event.</summary>
    /// <response code="201">The event was created.</response>
    /// <response code="400">The request failed validation, the department doesn't exist, or a holiday already exists on that date.</response>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request, CancellationToken cancellationToken)
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

    /// <summary>
    /// Creates a batch of events in one request (e.g. loading a year's national holiday
    /// list) instead of one HTTP call per event. Each item goes through the same
    /// validation and creation path as POST /events, one at a time in array order, so
    /// one bad row doesn't fail the whole batch — the response reports success/failure
    /// per item by index. Because items are processed sequentially and each is saved
    /// before the next runs, business rules that depend on prior rows (e.g. two
    /// Holidays on the same date) are still caught even within the same batch.
    /// </summary>
    /// <response code="200">The batch was processed; check each item's Success flag.</response>
    /// <response code="400">The batch itself was malformed (empty, or over the size cap).</response>
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkCreate([FromBody] BulkCreateEventsRequest request, CancellationToken cancellationToken)
    {
        var envelopeValidation = await _bulkValidator.ValidateAsync(request, cancellationToken);
        if (!envelopeValidation.IsValid)
        {
            return BadRequest(BuildValidationError(envelopeValidation));
        }

        var results = new List<BulkCreateEventResult>(request.Events.Count);

        for (var index = 0; index < request.Events.Count; index++)
        {
            var item = request.Events[index];
            var itemValidation = await _createValidator.ValidateAsync(item, cancellationToken);
            if (!itemValidation.IsValid)
            {
                results.Add(new BulkCreateEventResult
                {
                    Index = index,
                    Success = false,
                    ErrorCode = "VALIDATION.FAILED",
                    Error = string.Join("; ", itemValidation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")),
                });
                continue;
            }

            var result = await _service.CreateAsync(item, actorId: null, cancellationToken);
            results.Add(result.IsSuccess
                ? new BulkCreateEventResult { Index = index, Success = true, Event = result.Value }
                : new BulkCreateEventResult { Index = index, Success = false, ErrorCode = result.ErrorCode, Error = result.Error });
        }

        var response = new BulkCreateEventsResponse
        {
            Results = results,
            SucceededCount = results.Count(r => r.Success),
            FailedCount = results.Count(r => !r.Success),
        };

        return Ok(new ApiResponse<BulkCreateEventsResponse> { Data = response });
    }

    /// <summary>Lists events with paging, search, sorting, and EventType/Department filtering.</summary>
    /// <response code="200">A page of events.</response>
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] EventListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<EventResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Lists events whose date range intersects the given calendar month, with optional EventType/Department filtering.</summary>
    /// <response code="200">A page of events for the month.</response>
    /// <response code="400">The request failed validation.</response>
    [HttpGet("month")]
    public async Task<IActionResult> GetForMonth([FromQuery] MonthlyEventQuery query, CancellationToken cancellationToken)
    {
        var validation = await _monthlyValidator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var paged = await _service.GetForMonthAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<EventResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Gets a single event by id.</summary>
    /// <response code="200">The event was found.</response>
    /// <response code="404">No event was found for the given id.</response>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Updates an event.</summary>
    /// <response code="200">The event was updated.</response>
    /// <response code="400">The request failed validation, the department doesn't exist, or a holiday already exists on that date.</response>
    /// <response code="404">No event was found for the given id.</response>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.UpdateAsync(id, request, actorId: null, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Soft-deletes an event.</summary>
    /// <response code="204">The event was deleted.</response>
    /// <response code="404">No event was found for the given id.</response>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(id, actorId: null, cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<EventResponse> Envelope(EventResponse? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            CalendarErrorCodes.NotFound => StatusCodes.Status404NotFound,
            CalendarErrorCodes.InvalidDepartment => StatusCodes.Status400BadRequest,
            CalendarErrorCodes.DuplicateHoliday => StatusCodes.Status400BadRequest,
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
