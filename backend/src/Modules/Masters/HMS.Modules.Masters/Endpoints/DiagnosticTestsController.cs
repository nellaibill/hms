using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Masters.Endpoints;

/// <summary>
/// Diagnostic Test master CRUD — billable laboratory/radiology tests and packages with their
/// standard price (e.g. "Complete Blood Count", ₹250), feeding Billing's Laboratory/Radiology
/// service dropdowns. Mirrors ConsultationTypesController's shape.
/// </summary>
[ApiController]
[Route("api/v1/masters/diagnostic-tests")]
public class DiagnosticTestsController : ControllerBase
{
    private readonly IDiagnosticTestService _service;
    private readonly IValidator<CreateDiagnosticTestRequest> _createValidator;
    private readonly IValidator<UpdateDiagnosticTestRequest> _updateValidator;

    public DiagnosticTestsController(
        IDiagnosticTestService service,
        IValidator<CreateDiagnosticTestRequest> createValidator,
        IValidator<UpdateDiagnosticTestRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Creates a new diagnostic test.</summary>
    /// <response code="201">The diagnostic test was created.</response>
    /// <response code="400">The request failed validation, or the name is already in use.</response>
    [Authorize]
    [RequirePermission("identity-administration.create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDiagnosticTestRequest request, CancellationToken cancellationToken)
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

    /// <summary>Lists diagnostic tests with paging, search, sorting, and service-type/outsourced/active-status filtering.</summary>
    /// <response code="200">A page of diagnostic tests.</response>
    [Authorize]
    [RequirePermission("identity-administration.view")]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] DiagnosticTestListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<DiagnosticTestResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Gets a single diagnostic test by id.</summary>
    /// <response code="200">The diagnostic test was found.</response>
    /// <response code="404">No diagnostic test was found for the given id.</response>
    [Authorize]
    [RequirePermission("identity-administration.view")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Updates a diagnostic test.</summary>
    /// <response code="200">The diagnostic test was updated.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No diagnostic test was found for the given id.</response>
    [Authorize]
    [RequirePermission("identity-administration.edit")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDiagnosticTestRequest request, CancellationToken cancellationToken)
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

    /// <summary>Soft-deletes a diagnostic test.</summary>
    /// <response code="204">The diagnostic test was deleted.</response>
    /// <response code="404">No diagnostic test was found for the given id.</response>
    [Authorize]
    [RequirePermission("identity-administration.delete")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(id, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<DiagnosticTestResponse> Envelope(DiagnosticTestResponse? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            MastersErrorCodes.NotFound => StatusCodes.Status404NotFound,
            MastersErrorCodes.DuplicateCode => StatusCodes.Status400BadRequest,
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
