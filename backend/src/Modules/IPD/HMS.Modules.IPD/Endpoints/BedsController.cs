using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.IPD.Endpoints;

[ApiController]
[Route("api/v1/ipd/beds")]
public class BedsController : ControllerBase
{
    private readonly IBedService _service;
    private readonly IValidator<CreateBedRequest> _createValidator;
    private readonly IValidator<UpdateBedRequest> _updateValidator;

    public BedsController(
        IBedService service,
        IValidator<CreateBedRequest> createValidator,
        IValidator<UpdateBedRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBedRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.CreateAsync(request, actorId: User.GetUserId(), cancellationToken);
        return !result.IsSuccess
            ? MapFailure(result.ErrorCode!, result.Error!)
            : CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, Envelope(result.Value));
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] BedListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };
        return Ok(new ApiResponse<IReadOnlyList<BedResponse>> { Data = paged.Items, Meta = meta });
    }

    // Must be registered before GetById's "{id:guid}" route so "available" isn't parsed as a
    // (invalid) guid route value.
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable([FromQuery] Guid? wardId, CancellationToken cancellationToken)
    {
        var result = await _service.GetAvailableAsync(wardId, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<BedResponse>> { Data = result.Value });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBedRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.UpdateAsync(id, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(id, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<BedResponse> Envelope(BedResponse? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            IPDErrorCodes.NotFound => StatusCodes.Status404NotFound,
            IPDErrorCodes.DuplicateBedNumber => StatusCodes.Status409Conflict,
            IPDErrorCodes.BedOccupied => StatusCodes.Status409Conflict,
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
