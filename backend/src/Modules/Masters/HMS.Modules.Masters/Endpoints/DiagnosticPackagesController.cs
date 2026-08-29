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
/// Diagnostic Package master CRUD, plus per-row test add/remove (mirrors PatientsController's
/// Allergy add/remove one-row-at-a-time shape) — a package bundles several DiagnosticService
/// tests at one discounted TotalPrice (e.g. "Master Health Checkup"). Under the "diagnostics.*"
/// permission key, same as this module's other new controllers.
/// </summary>
[ApiController]
[Route("api/v1/masters/diagnostic-packages")]
public class DiagnosticPackagesController : ControllerBase
{
    private readonly IDiagnosticPackageService _service;
    private readonly IValidator<CreateDiagnosticPackageRequest> _createValidator;
    private readonly IValidator<UpdateDiagnosticPackageRequest> _updateValidator;
    private readonly IValidator<AddDiagnosticPackageItemRequest> _addItemValidator;

    public DiagnosticPackagesController(
        IDiagnosticPackageService service,
        IValidator<CreateDiagnosticPackageRequest> createValidator,
        IValidator<UpdateDiagnosticPackageRequest> updateValidator,
        IValidator<AddDiagnosticPackageItemRequest> addItemValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _addItemValidator = addItemValidator;
    }

    /// <summary>Creates a new diagnostic package with its initial set of tests.</summary>
    /// <response code="201">The diagnostic package was created.</response>
    /// <response code="400">The request failed validation (including an empty ServiceIds list), the code is already in use, or a ServiceId reference is invalid.</response>
    [Authorize]
    [RequirePermission("diagnostics.create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDiagnosticPackageRequest request, CancellationToken cancellationToken)
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

    /// <summary>Lists diagnostic packages with paging, search, sorting, and active-status filtering.</summary>
    /// <response code="200">A page of diagnostic packages.</response>
    [Authorize]
    [RequirePermission("diagnostics.view")]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] DiagnosticPackageListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<DiagnosticPackageResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Gets a single diagnostic package (with its items) by id.</summary>
    /// <response code="200">The diagnostic package was found.</response>
    /// <response code="404">No diagnostic package was found for the given id.</response>
    [Authorize]
    [RequirePermission("diagnostics.view")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Updates a diagnostic package's own fields (Code/Name/Description/TotalPrice/IsActive) — items are managed separately via the endpoints below.</summary>
    /// <response code="200">The diagnostic package was updated.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No diagnostic package was found for the given id.</response>
    [Authorize]
    [RequirePermission("diagnostics.edit")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDiagnosticPackageRequest request, CancellationToken cancellationToken)
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

    /// <summary>Soft-deletes a diagnostic package.</summary>
    /// <response code="204">The diagnostic package was deleted.</response>
    /// <response code="404">No diagnostic package was found for the given id.</response>
    [Authorize]
    [RequirePermission("diagnostics.delete")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(id, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Adds one test to the package ("Add another Test" on the package detail page).</summary>
    /// <response code="200">The test was added; returns the updated package.</response>
    /// <response code="400">The request failed validation, or the ServiceId reference is invalid.</response>
    /// <response code="404">No diagnostic package was found for the given id.</response>
    [Authorize]
    [RequirePermission("diagnostics.edit")]
    [HttpPost("{id:guid}/items")]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] AddDiagnosticPackageItemRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(BuildRequestRequiredError());
        }

        var validation = await _addItemValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.AddItemAsync(id, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Removes one test from the package.</summary>
    /// <response code="200">The test was removed; returns the updated package.</response>
    /// <response code="404">No diagnostic package, or no matching item, was found.</response>
    [Authorize]
    [RequirePermission("diagnostics.edit")]
    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid id, Guid itemId, CancellationToken cancellationToken)
    {
        var result = await _service.RemoveItemAsync(id, itemId, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<DiagnosticPackageResponse> Envelope(DiagnosticPackageResponse? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            MastersErrorCodes.NotFound => StatusCodes.Status404NotFound,
            MastersErrorCodes.DuplicateCode => StatusCodes.Status400BadRequest,
            MastersErrorCodes.InvalidPackageItemService => StatusCodes.Status400BadRequest,
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
