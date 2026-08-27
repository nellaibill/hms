using HMS.Shared.Kernel;

namespace HMS.Modules.Notifications.Contracts;

/// <summary>
/// Query parameters for GET /api/v1/notifications — pagination comes from
/// <see cref="PagedRequest"/>; <see cref="IsRead"/> is this endpoint's own filter. Always
/// implicitly scoped to the caller's own UserId (from the JWT) — there is no UserId
/// parameter here, by design (see NotificationsController's doc comment).
/// </summary>
public class NotificationListQuery : PagedRequest
{
    public bool? IsRead { get; set; }
}
