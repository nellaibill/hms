using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Notifications.Application;
using HMS.Modules.Notifications.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Notifications.Endpoints;

/// <summary>
/// Admin screen for authoring notification content without a redeploy — every action
/// requires "engagement.*" (reused, not a new permission-catalog entry, per
/// docs/DecisionLog.md ADR-029) and the tenant to have "messages-and-notifications" enabled.
/// </summary>
[ApiController]
[RequireFeature("messages-and-notifications")]
[Route("api/v1/notification-templates")]
public class NotificationTemplatesController : ControllerBase
{
    private readonly INotificationTemplateService _templateService;
    private readonly IValidator<CreateNotificationTemplateRequest> _createValidator;
    private readonly IValidator<UpdateNotificationTemplateRequest> _updateValidator;

    public NotificationTemplatesController(
        INotificationTemplateService templateService,
        IValidator<CreateNotificationTemplateRequest> createValidator,
        IValidator<UpdateNotificationTemplateRequest> updateValidator)
    {
        _templateService = templateService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Lists templates — optionally filtered to only active/inactive.</summary>
    /// <response code="200">The template list.</response>
    [Authorize]
    [RequirePermission("engagement.view")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var templates = await _templateService.GetAllAsync(isActive, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<NotificationTemplateResponse>> { Data = templates });
    }

    /// <summary>Creates a template for one (event, channel) pair.</summary>
    /// <response code="201">The template was created.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="409">A template already exists for this TemplateKey/Channel pair.</response>
    [Authorize]
    [RequirePermission("engagement.create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNotificationTemplateRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _templateService.CreateAsync(request, actorId: User.GetUserId(), cancellationToken);
        return !result.IsSuccess
            ? MapFailure(result.ErrorCode!, result.Error!)
            : StatusCode(StatusCodes.Status201Created, new ApiResponse<NotificationTemplateResponse> { Data = result.Value });
    }

    /// <summary>Updates a template's content and active state.</summary>
    /// <response code="200">The template was updated.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No template was found for the given id.</response>
    [Authorize]
    [RequirePermission("engagement.edit")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateNotificationTemplateRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _templateService.UpdateAsync(id, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(new ApiResponse<NotificationTemplateResponse> { Data = result.Value }) : MapFailure(result.ErrorCode!, result.Error!);
    }

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            NotificationErrorCodes.TemplateNotFound => StatusCodes.Status404NotFound,
            NotificationErrorCodes.DuplicateTemplate => StatusCodes.Status409Conflict,
            NotificationErrorCodes.EmailTemplateRequiresSubject => StatusCodes.Status400BadRequest,
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
        ValidationErrors = validation.Errors
            .Select(e => new ValidationErrorItem { Field = e.PropertyName, Message = e.ErrorMessage })
            .ToList(),
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };
}
