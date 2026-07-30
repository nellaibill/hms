using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Identity.Application;
using HMS.Modules.Identity.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Identity.Endpoints;

[ApiController]
[Route("api/v1/roles")]
public sealed class RolesController : ControllerBase
{
    private readonly IRoleService _service;
    private readonly IValidator<CreateRoleRequest> _createValidator;
    private readonly IValidator<UpdateRoleRequest> _updateValidator;

    public RolesController(
        IRoleService service,
        IValidator<CreateRoleRequest> createValidator,
        IValidator<UpdateRoleRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RoleResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] RoleListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetPagedAsync(query, cancellationToken);

        var meta = new PaginationMeta
        {
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages,
        };

        return Ok(new ApiResponse<IReadOnlyList<RoleResponse>> { Data = result.Items, Meta = meta });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        return Ok(Envelope(result.Value));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);

        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        Guid? actorId = null;

        var result = await _service.CreateAsync(request, actorId, cancellationToken);

        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            Envelope(result.Value));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);

        if (!validation.IsValid)
        {
             return BadRequest(BuildValidationError(validation));
        }

        Guid? actorId = null;

        var result = await _service.UpdateAsync(id, request, actorId, cancellationToken);

        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        return Ok(Envelope(result.Value));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        Guid? actorId = null;

        var result = await _service.DeleteAsync(id, actorId, cancellationToken);

        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(
        Guid id,
        CancellationToken cancellationToken)
    {
        Guid? actorId = null;

        var result = await _service.ActivateAsync(id, actorId, cancellationToken);

        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        return Ok(Envelope(result.Value));
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        Guid? actorId = null;

        var result = await _service.DeactivateAsync(id, actorId, cancellationToken);

        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        return Ok(Envelope(result.Value));
    }

    private static ApiResponse<T> Envelope<T>(T? data)
        => new()
        {
            Data = data
        };

    private IActionResult MapFailure(string errorCode, string message)
{
        var status = errorCode switch
        {
            RoleErrorCodes.NotFound => StatusCodes.Status404NotFound,

            RoleErrorCodes.DuplicateName => StatusCodes.Status409Conflict,
            RoleErrorCodes.SystemRoleDeletionForbidden => StatusCodes.Status409Conflict,

            RoleErrorCodes.InvalidPermission => StatusCodes.Status400BadRequest,

            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(status, new ApiErrorResponse
        {
            ErrorCode = errorCode,
            Message = message,
            CorrelationId = HttpContext.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        });
}
    private ApiErrorResponse BuildValidationError(ValidationResult validation) => new()
    {
        ErrorCode = "VALIDATION.FAILED",
        Message = "One or more validation errors occurred.",
        ValidationErrors = validation.Errors
            .Select(e => new ValidationErrorItem
            {
                Field = e.PropertyName,
                Message = e.ErrorMessage
            })
            .ToList(),
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };
}