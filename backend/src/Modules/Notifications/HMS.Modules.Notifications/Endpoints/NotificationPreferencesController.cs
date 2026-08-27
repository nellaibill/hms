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
/// The caller's own channel preferences by category. Self-service only — scoped to the
/// caller's own UserId from the JWT, same reasoning as NotificationsController's "my
/// notifications" actions, so no RequirePermission gate beyond [Authorize].
/// </summary>
[ApiController]
[RequireFeature("messages-and-notifications")]
[Route("api/v1/notification-preferences")]
public class NotificationPreferencesController : ControllerBase
{
    private readonly INotificationPreferenceService _preferenceService;
    private readonly IValidator<UpdateNotificationPreferenceRequest> _updateValidator;

    public NotificationPreferencesController(
        INotificationPreferenceService preferenceService,
        IValidator<UpdateNotificationPreferenceRequest> updateValidator)
    {
        _preferenceService = preferenceService;
        _updateValidator = updateValidator;
    }

    /// <summary>The caller's saved preferences. A category with no row here uses the
    /// default (in-app + email on, sms off) — see NotificationPreference's own doc
    /// comment.</summary>
    /// <response code="200">The caller's saved preferences.</response>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var preferences = await _preferenceService.GetMyPreferencesAsync(User.GetUserId()!.Value, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<NotificationPreferenceResponse>> { Data = preferences });
    }

    /// <summary>Upserts the caller's preference for one category.</summary>
    /// <response code="200">The preference was saved.</response>
    /// <response code="400">The request failed validation.</response>
    [Authorize]
    [HttpPut]
    public async Task<IActionResult> Upsert([FromBody] UpdateNotificationPreferenceRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var preference = await _preferenceService.UpsertMyPreferenceAsync(User.GetUserId()!.Value, request, cancellationToken);
        return Ok(new ApiResponse<NotificationPreferenceResponse> { Data = preference });
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
