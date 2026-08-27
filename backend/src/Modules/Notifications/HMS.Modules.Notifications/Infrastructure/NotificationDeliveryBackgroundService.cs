using HMS.Modules.Identity.Application;
using HMS.Modules.Notifications.Application.Abstractions;
using HMS.Modules.Notifications.Contracts;
using HMS.Shared.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Notifications.Infrastructure;

/// <summary>
/// Drains INotificationDeliveryQueue and sends each Email/Sms delivery, updating its
/// NotificationDelivery row's status. Registered as a hosted service (singleton), so it
/// resolves every scoped dependency — including Identity's public IUserService, used to
/// resolve a recipient's actual email/phone number — through a fresh DI scope per item, the
/// same pattern HMS.Modules.Documents.Infrastructure.DocumentScanBackgroundService already
/// uses for a singleton service that needs scoped dependencies.
/// </summary>
internal class NotificationDeliveryBackgroundService : BackgroundService
{
    private readonly INotificationDeliveryQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationDeliveryBackgroundService> _logger;

    public NotificationDeliveryBackgroundService(INotificationDeliveryQueue queue, IServiceScopeFactory scopeFactory, ILogger<NotificationDeliveryBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await DeliverOneAsync(item, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A failure here must not crash the whole background service — it would
                // silently stop delivering every subsequent queued item for the rest of the
                // process's lifetime. This one delivery is simply left in whatever state it
                // was in (typically still Pending) for now — mirrors
                // DocumentScanBackgroundService's identical top-level guard.
                _logger.LogError(ex, "Failed to process notification delivery {NotificationDeliveryId}.", item.NotificationDeliveryId);
            }
        }
    }

    private async Task DeliverOneAsync(NotificationDeliveryQueueItem item, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        // Must happen before anything resolves a tenant-aware DbContext (the repositories
        // below) — this scope has no HTTP request of its own for TenantResolutionMiddleware
        // to have populated ITenantContext from (see NotificationDeliveryQueueItem's own doc
        // comment).
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantContext.SetTenant(item.TenantId, item.ConnectionString);

        var deliveryRepository = scope.ServiceProvider.GetRequiredService<INotificationDeliveryRepository>();
        var delivery = await deliveryRepository.GetByIdAsync(item.NotificationDeliveryId, cancellationToken);
        if (delivery is null)
        {
            _logger.LogWarning("NotificationDelivery {NotificationDeliveryId} was queued but no longer exists.", item.NotificationDeliveryId);
            return;
        }

        var recipientRepository = scope.ServiceProvider.GetRequiredService<INotificationRecipientRepository>();
        var recipient = await recipientRepository.GetByIdAsync(delivery.NotificationRecipientId, cancellationToken);
        if (recipient is null)
        {
            _logger.LogWarning("NotificationDelivery {NotificationDeliveryId}'s recipient no longer exists.", item.NotificationDeliveryId);
            return;
        }

        var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var notification = await notificationRepository.GetByIdAsync(recipient.NotificationId, cancellationToken);
        if (notification is null)
        {
            _logger.LogWarning("NotificationDelivery {NotificationDeliveryId}'s notification no longer exists.", item.NotificationDeliveryId);
            return;
        }

        // Identity's public seam (docs/DeveloperHandbook.md §19's cross-module rule) —
        // Notifications has no identity.users of its own to read an email/phone from.
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var userResult = await userService.GetByIdAsync(recipient.UserId, cancellationToken);
        if (!userResult.IsSuccess)
        {
            delivery.MarkFailed($"Recipient user '{recipient.UserId}' no longer exists.");
            await deliveryRepository.SaveChangesAsync(cancellationToken);
            return;
        }

        var user = userResult.Value!;

        try
        {
            if (delivery.Channel == NotificationChannel.Email)
            {
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                await emailSender.SendAsync(user.Email, notification.Title, notification.Body, cancellationToken);
            }
            else if (delivery.Channel == NotificationChannel.Sms)
            {
                if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                {
                    delivery.MarkFailed("Recipient has no phone number on file.");
                    await deliveryRepository.SaveChangesAsync(cancellationToken);
                    return;
                }

                var smsSender = scope.ServiceProvider.GetRequiredService<ISmsSender>();
                await smsSender.SendAsync(user.PhoneNumber, notification.Body, cancellationToken);
            }

            delivery.MarkSent(DateTime.UtcNow);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            delivery.MarkFailed(ex.Message);
        }

        await deliveryRepository.SaveChangesAsync(cancellationToken);
    }
}
