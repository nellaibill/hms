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
/// Diagnostic Category master CRUD — groupings (e.g. "Hematology", "CT Scan") for
/// DiagnosticService rows, feeding the Central Laboratory/Radiology modules' service catalogs.
/// Mirrors DiagnosticTestsController's shape, but under the "diagnostics.*" permission key
/// (not "identity-administration.*", which DiagnosticTestsController uses — a pre-existing
/// inconsistency this controller deliberately doesn't repeat).
/// </summary>
[ApiController]
[Route("api/v1/masters/diagnostic-categories")]
public class DiagnosticCategoriesController : ControllerBase
{
    private readonly IDiagnosticCategoryService _service;
    private readonly IValidator<CreateDiagnosticCategoryRequest> _createValidator;
    private readonly IValidator<UpdateDiagnosticCategoryRequest> _updateValidator;

    public DiagnosticCategoriesController(
        IDiagnosticCategoryService service,
        IValidator<CreateDiagnosticCategoryRequest> createValidator,
        IValidator<UpdateDiagnosticCategoryRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Creates a new diagnostic category.</summary>
    /// <response code="201">The diagnostic category was created.</response>
    /// <response code="400">The request failed validation, or the code is already in use.</response>
    [Authorize]
    [RequirePermission("diagnostics.create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDiagnosticCategoryRequest request, CancellationToken cancellationToken)
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

    /// <summary>Lists diagnostic categories with paging, search, sorting, and active-status filtering.</summary>
    /// <response code="200">A page of diagnostic categories.</response>
    [Authorize]
    [RequirePermission("diagnostics.view")]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] DiagnosticCategoryListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<DiagnosticCategoryResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Gets a single diagnostic category by id.</summary>
    /// <response code="200">The diagnostic category was found.</response>
    /// <response code="404">No diagnostic category was found for the given id.</response>
    [Authorize]
    [RequirePermission("diagnostics.view")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Updates a diagnostic category.</summary>
    /// <response code="200">The diagnostic category was updated.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No diagnostic category was found for the given id.</response>
    [Authorize]
    [RequirePermission("diagnostics.edit")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDiagnosticCategoryRequest request, CancellationToken cancellationToken)
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

    /// <summary>Soft-deletes a diagnostic category.</summary>
    /// <response code="204">The diagnostic category was deleted.</response>
    /// <response code="404">No diagnostic category was found for the given id.</response>
    [Authorize]
    [RequirePermission("diagnostics.delete")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(id, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<DiagnosticCategoryResponse> Envelope(DiagnosticCategoryResponse? data) => new() { Data = data };

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
