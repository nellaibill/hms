using HMS.Modules.Notifications.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Notifications.Application;

/// <summary>
/// Public (not internal): this module's one deliberate, narrow seam — the same CS0051
/// reasoning as HMS.Modules.Identity.IUserService (NotificationsController, which ASP.NET
/// Core requires to be public with a public constructor, takes this as a dependency).
/// It is also, deliberately, the single method every other HMS module will call in-process
/// to raise a notification (a later phase wires the real call sites in Appointments/
/// Patients/Billing/Pharmacy/IPD) — the same in-process pattern HMS.Modules.Pharmacy
/// already uses for HMS.Modules.Billing's IInvoiceService, not an event bus (see
/// docs/DecisionLog.md, ADR-029).
/// </summary>
public interface INotificationService
{
    Task<Result<NotificationBroadcastResponse>> NotifyAsync(NotifyRequest request, Guid? actorId, CancellationToken cancellationToken);

    /// <summary>Always scoped to <paramref name="userId"/> — there is no way to fetch
    /// another user's notifications through this method, by design.</summary>
    Task<PagedResult<NotificationResponse>> GetMyNotificationsAsync(Guid userId, bool? isRead, int page, int pageSize, CancellationToken cancellationToken);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Fails with <see cref="NotificationErrorCodes.NotFound"/> if
    /// <paramref name="notificationRecipientId"/> doesn't exist or doesn't belong to
    /// <paramref name="userId"/> — see that error code's own doc comment.</summary>
    Task<Result> MarkAsReadAsync(Guid notificationRecipientId, Guid userId, CancellationToken cancellationToken);

    Task<Result> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken);
}
