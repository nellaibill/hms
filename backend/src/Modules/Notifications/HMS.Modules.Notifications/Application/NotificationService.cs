using HMS.Modules.Notifications.Application.Abstractions;
using HMS.Modules.Notifications.Application.Mapping;
using HMS.Modules.Notifications.Contracts;
using HMS.Modules.Notifications.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Notifications.Application;

/// <summary>
/// Orchestrates Notifications use cases: expected failures (not found / not yours) are
/// returned as <see cref="Result"/> failures, never thrown — see docs/Architecture.md's
/// exception handling strategy. NotifyAsync writes the in-app channel synchronously
/// (Notification + NotificationRecipient rows) and queues Email/Sms as best-effort
/// NotificationDelivery rows for NotificationDeliveryBackgroundService to actually send —
/// see that type's own doc comment for why a request never waits on an outbound
/// email/SMS call.
/// </summary>
internal class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationRecipientRepository _recipientRepository;
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly INotificationDeliveryRepository _deliveryRepository;
    private readonly INotificationDeliveryQueue _deliveryQueue;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        INotificationRecipientRepository recipientRepository,
        INotificationTemplateRepository templateRepository,
        INotificationPreferenceRepository preferenceRepository,
        INotificationDeliveryRepository deliveryRepository,
        INotificationDeliveryQueue deliveryQueue,
        ITenantContext tenantContext,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _recipientRepository = recipientRepository;
        _templateRepository = templateRepository;
        _preferenceRepository = preferenceRepository;
        _deliveryRepository = deliveryRepository;
        _deliveryQueue = deliveryQueue;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<NotificationBroadcastResponse>> NotifyAsync(NotifyRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var recipientUserIds = request.RecipientUserIds.Distinct().ToList();
        if (recipientUserIds.Count == 0)
        {
            return Result<NotificationBroadcastResponse>.Failure(
                NotificationErrorCodes.NoRecipients,
                "At least one recipient is required.");
        }

        var body = request.Body;
        if (string.IsNullOrWhiteSpace(body))
        {
            // Body omitted — render the InApp-channel template instead (see NotifyRequest.
            // Body's own doc comment). Email/Sms template resolution belongs to the
            // background delivery pipeline (a later phase), not here.
            var template = await _templateRepository.GetByKeyAndChannelAsync(request.TemplateKey, NotificationChannel.InApp, cancellationToken);
            if (template is null || !template.IsActive)
            {
                return Result<NotificationBroadcastResponse>.Failure(
                    NotificationErrorCodes.TemplateNotFound,
                    $"No active InApp template exists for '{request.TemplateKey}', and no Body was supplied directly.");
            }

            body = TemplateRenderer.Render(template.BodyTemplate, request.Placeholders);
        }

        var notification = Notification.Create(
            request.TemplateKey,
            request.Category,
            request.Title,
            body,
            request.SourceModule,
            request.SourceEntityType,
            request.SourceEntityId,
            request.Severity,
            actorId);

        await _notificationRepository.AddAsync(notification, cancellationToken);

        var recipients = recipientUserIds
            .Select(userId => NotificationRecipient.Create(notification.Id, userId, actorId))
            .ToList();
        await _recipientRepository.AddRangeAsync(recipients, cancellationToken);

        var deliveries = new List<NotificationDelivery>();
        foreach (var recipient in recipients)
        {
            var (emailEnabled, smsEnabled) = await ResolveChannelsAsync(recipient.UserId, request.Category, request.Severity, cancellationToken);

            if (emailEnabled)
            {
                deliveries.Add(NotificationDelivery.Create(recipient.Id, NotificationChannel.Email, actorId));
            }

            if (smsEnabled)
            {
                deliveries.Add(NotificationDelivery.Create(recipient.Id, NotificationChannel.Sms, actorId));
            }
        }

        if (deliveries.Count > 0)
        {
            await _deliveryRepository.AddRangeAsync(deliveries, cancellationToken);
        }

        // One SaveChanges for the Notification, its NotificationRecipient rows, and any
        // NotificationDelivery rows — all three repositories share the same DbContext/scope,
        // so this commits atomically (never a Notification with no recipients, or a
        // recipient with a delivery row that was never actually persisted). Mirrors the
        // two-phase-save bug fixed in HMS.Modules.Pharmacy's DispenseService
        // (docs/DecisionLog.md).
        await _notificationRepository.SaveChangesAsync(cancellationToken);

        // Enqueued only after the commit above — the background reader re-fetches each
        // delivery by id, so it must already exist in the database (see
        // NotificationDeliveryQueueItem's own doc comment). By this point ITenantContext is
        // guaranteed resolved: constructing NotificationsDbContext for the AddAsync calls
        // above would already have thrown otherwise (see NotificationsModule's registration).
        foreach (var delivery in deliveries)
        {
            await _deliveryQueue.EnqueueAsync(
                new NotificationDeliveryQueueItem(delivery.Id, _tenantContext.TenantId!.Value, _tenantContext.ConnectionString!),
                cancellationToken);
        }

        _logger.LogInformation(
            "Created notification {NotificationId} for {RecipientCount} recipient(s), {DeliveryCount} queued Email/Sms deliveries",
            notification.Id,
            recipients.Count,
            deliveries.Count);

        return Result<NotificationBroadcastResponse>.Success(new NotificationBroadcastResponse
        {
            NotificationId = notification.Id,
            RecipientCount = recipients.Count,
        });
    }

    /// <summary>Email/Sms opt-in for one recipient — Emergency severity bypasses
    /// NotificationPreferences entirely (every channel, always), per the design doc.
    /// A missing preference row uses the same defaults NotificationPreference.Create
    /// itself would set (email on, sms off) rather than auto-creating a row just to read
    /// them.</summary>
    private async Task<(bool EmailEnabled, bool SmsEnabled)> ResolveChannelsAsync(Guid userId, string category, NotificationSeverity severity, CancellationToken cancellationToken)
    {
        if (severity == NotificationSeverity.Emergency)
        {
            return (true, true);
        }

        var preference = await _preferenceRepository.GetByUserAndCategoryAsync(userId, category, cancellationToken);
        return preference is null ? (true, false) : (preference.EmailEnabled, preference.SmsEnabled);
    }

    public async Task<PagedResult<NotificationResponse>> GetMyNotificationsAsync(Guid userId, bool? isRead, int page, int pageSize, CancellationToken cancellationToken)
    {
        var recipientsPage = await _recipientRepository.GetByUserAsync(userId, isRead, page, pageSize, cancellationToken);

        // One batched Notification lookup for the whole page rather than one per row (N+1)
        // — mirrors HMS.Modules.Identity.UserService.GetPagedAsync's role-batching.
        var notificationIds = recipientsPage.Items.Select(r => r.NotificationId).Distinct();
        var notifications = await _notificationRepository.GetManyByIdsAsync(notificationIds, cancellationToken);
        var notificationById = notifications.ToDictionary(n => n.Id);

        var mapped = recipientsPage.Items
            .Where(r => notificationById.ContainsKey(r.NotificationId))
            .Select(r => r.ToResponse(notificationById[r.NotificationId]))
            .ToList();

        return new PagedResult<NotificationResponse>(mapped, recipientsPage.Page, recipientsPage.PageSize, recipientsPage.TotalCount);
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken)
        => _recipientRepository.GetUnreadCountAsync(userId, cancellationToken);

    public async Task<Result> MarkAsReadAsync(Guid notificationRecipientId, Guid userId, CancellationToken cancellationToken)
    {
        var recipient = await _recipientRepository.GetByIdAsync(notificationRecipientId, cancellationToken);
        if (recipient is null || recipient.UserId != userId)
        {
            return Result.Failure(NotificationErrorCodes.NotFound, $"Notification '{notificationRecipientId}' was not found.");
        }

        recipient.MarkAsRead(DateTime.UtcNow);
        await _recipientRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken)
    {
        var unread = await _recipientRepository.GetUnreadByUserAsync(userId, cancellationToken);
        if (unread.Count == 0)
        {
            return Result.Success();
        }

        var readAt = DateTime.UtcNow;
        foreach (var recipient in unread)
        {
            recipient.MarkAsRead(readAt);
        }

        await _recipientRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
