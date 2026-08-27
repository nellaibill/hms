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
/// The Notifications module's HTTP surface for this phase: "my notifications" (list,
/// unread-count, mark-read/mark-all-read) plus one admin manual-send action. Every
/// "my notifications" action is scoped to the caller's own UserId, read from the JWT via
/// ClaimsPrincipalExtensions.GetUserId — there is no way to pass another user's id to any
/// of them, so no separate RequirePermission gate is needed beyond [Authorize] (mirrors
/// AuthenticationService's self-service ChangePasswordAsync reasoning). Every action
/// requires the tenant to have the "messages-and-notifications" feature enabled.
/// </summary>
[ApiController]
[RequireFeature("messages-and-notifications")]
[Route("api/v1/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IValidator<NotifyRequest> _notifyValidator;

    public NotificationsController(INotificationService notificationService, IValidator<NotifyRequest> notifyValidator)
    {
        _notificationService = notificationService;
        _notifyValidator = notifyValidator;
    }

    /// <summary>Lists the caller's own notifications, newest first — optionally filtered to
    /// only unread (<c>?isRead=false</c>).</summary>
    /// <response code="200">A page of the caller's notifications.</response>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] NotificationListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _notificationService.GetMyNotificationsAsync(User.GetUserId()!.Value, query.IsRead, query.Page, query.PageSize, cancellationToken);

        var meta = new PaginationMeta
        {
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            TotalPages = paged.TotalPages,
        };

        return Ok(new ApiResponse<IReadOnlyList<NotificationResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>The caller's unread-notification count, for the notification bell badge.</summary>
    /// <response code="200">The unread count.</response>
    [Authorize]
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var count = await _notificationService.GetUnreadCountAsync(User.GetUserId()!.Value, cancellationToken);
        return Ok(new ApiResponse<UnreadCountResponse> { Data = new UnreadCountResponse { Count = count } });
    }

    /// <summary>Marks one of the caller's own notifications as read. Idempotent — marking an
    /// already-read notification succeeds without error.</summary>
    /// <response code="204">The notification was marked read.</response>
    /// <response code="404">No such notification exists for the caller.</response>
    [Authorize]
    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var result = await _notificationService.MarkAsReadAsync(id, User.GetUserId()!.Value, cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Marks every one of the caller's unread notifications as read.</summary>
    /// <response code="204">All of the caller's notifications are now read.</response>
    [Authorize]
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await _notificationService.MarkAllAsReadAsync(User.GetUserId()!.Value, cancellationToken);
        return NoContent();
    }

    /// <summary>Manually raises a notification to an explicit list of recipients (e.g. an
    /// emergency broadcast). Most notifications are created by another module calling
    /// <see cref="INotificationService.NotifyAsync"/> in-process, not through this endpoint —
    /// see that method's own doc comment.</summary>
    /// <response code="201">The notification was created.</response>
    /// <response code="400">The request failed validation.</response>
    [Authorize]
    [RequirePermission("engagement.create")]
    [HttpPost]
    public async Task<IActionResult> Notify([FromBody] NotifyRequest request, CancellationToken cancellationToken)
    {
        var validation = await _notifyValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _notificationService.NotifyAsync(request, actorId: User.GetUserId(), cancellationToken);
        return !result.IsSuccess
            ? MapFailure(result.ErrorCode!, result.Error!)
            : StatusCode(StatusCodes.Status201Created, new ApiResponse<NotificationBroadcastResponse> { Data = result.Value });
    }

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            NotificationErrorCodes.NotFound => StatusCodes.Status404NotFound,
            NotificationErrorCodes.NoRecipients => StatusCodes.Status400BadRequest,
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
