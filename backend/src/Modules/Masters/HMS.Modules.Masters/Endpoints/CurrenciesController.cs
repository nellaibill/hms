using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Masters.Endpoints;

/// <summary>
/// Currency master CRUD (no hard delete — see docs/03_Masters_ERD's is_active note;
/// deactivate via PUT instead). "Actor" (created/updated-by) is read from the caller's
/// JWT via ClaimsPrincipalExtensions.GetUserId — same as HMS.Modules.Patients.PatientsController.
/// </summary>
[ApiController]
[Route("api/v1/masters/currencies")]
public class CurrenciesController : ControllerBase
{
    private readonly ICurrencyService _service;
    private readonly IValidator<CreateCurrencyRequest> _createValidator;
    private readonly IValidator<UpdateCurrencyRequest> _updateValidator;

    public CurrenciesController(ICurrencyService service, IValidator<CreateCurrencyRequest> createValidator, IValidator<UpdateCurrencyRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Creates a new currency.</summary>
    /// <response code="201">The currency was created.</response>
    /// <response code="400">The request failed validation, or the currency code is already in use.</response>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCurrencyRequest request, CancellationToken cancellationToken)
    {
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

    /// <summary>Updates a currency.</summary>
    /// <response code="200">The currency was updated.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No currency was found for the given id.</response>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCurrencyRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.UpdateAsync(id, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Gets a single currency by id.</summary>
    /// <response code="200">The currency was found.</response>
    /// <response code="404">No currency was found for the given id.</response>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Lists currencies with paging, search, and active-status filtering.</summary>
    /// <response code="200">A page of currencies.</response>
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] CurrencyListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);

        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<CurrencyResponse>> { Data = paged.Items, Meta = meta });
    }

    private static ApiResponse<CurrencyResponse> Envelope(CurrencyResponse? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            MastersErrorCodes.NotFound => StatusCodes.Status404NotFound,
            MastersErrorCodes.DuplicateCode => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest,
        };

        var error = new ApiErrorResponse
        {
            ErrorCode = errorCode,
            Message = message,
            CorrelationId = HttpContext.GetCorrelationId(),
            Timestamp = DateTime.UtcNow,
        };

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
