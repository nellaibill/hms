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
/// Designation master CRUD — a staff job title/designation, consumed by HR's Employee entity
/// (DesignationId). A near-exact clone of DepartmentsController; same permission key
/// ("identity-administration.*", uniform across every Masters controller regardless of
/// domain — see docs/DecisionLog.md).
/// </summary>
[ApiController]
[Route("api/v1/masters/designations")]
public class DesignationsController : ControllerBase
{
    private readonly IDesignationService _service;
    private readonly IValidator<CreateDesignationRequest> _createValidator;
    private readonly IValidator<UpdateDesignationRequest> _updateValidator;

    public DesignationsController(
        IDesignationService service,
        IValidator<CreateDesignationRequest> createValidator,
        IValidator<UpdateDesignationRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Creates a new designation.</summary>
    /// <response code="201">The designation was created.</response>
    /// <response code="400">The request failed validation, or the code is already in use.</response>
    [Authorize]
    [RequirePermission("identity-administration.create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDesignationRequest request, CancellationToken cancellationToken)
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

    /// <summary>Lists designations with paging, search, sorting, and active-status filtering.</summary>
    /// <response code="200">A page of designations.</response>
    [Authorize]
    [RequirePermission("identity-administration.view")]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] DesignationListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<DesignationResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Gets a single designation by id.</summary>
    /// <response code="200">The designation was found.</response>
    /// <response code="404">No designation was found for the given id.</response>
    [Authorize]
    [RequirePermission("identity-administration.view")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Updates a designation.</summary>
    /// <response code="200">The designation was updated.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No designation was found for the given id.</response>
    [Authorize]
    [RequirePermission("identity-administration.edit")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDesignationRequest request, CancellationToken cancellationToken)
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

    /// <summary>Soft-deletes a designation.</summary>
    /// <response code="204">The designation was deleted.</response>
    /// <response code="404">No designation was found for the given id.</response>
    [Authorize]
    [RequirePermission("identity-administration.delete")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(id, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<DesignationResponse> Envelope(DesignationResponse? data) => new() { Data = data };

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
