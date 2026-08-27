using HMS.Modules.Notifications.Contracts;
using HMS.Modules.Notifications.Domain;

namespace HMS.Modules.Notifications.Application.Mapping;

/// <summary>
/// Manual entity-to-DTO mapping. A single pair of entities doesn't justify a mapping
/// library (Mapster/AutoMapper) at MVP scale — see docs/DecisionLog.md, ADR-003.
/// </summary>
internal static class NotificationMappingExtensions
{
    /// <summary>Combines a NotificationRecipient (the "my notifications" row — Id, IsRead,
    /// ReadAt) with its owning Notification (the rendered content) into one API-facing
    /// shape. The caller is responsible for having already loaded the matching
    /// <paramref name="notification"/> (see NotificationService.GetMyNotificationsAsync's
    /// batched lookup) — passed in rather than navigated to, since NotificationRecipient
    /// carries no EF navigation property to Notification (see that entity's own doc
    /// comment on why this codebase's cross-entity FKs skip navigation properties here).</summary>
    public static NotificationResponse ToResponse(this NotificationRecipient recipient, Notification notification) => new()
    {
        Id = recipient.Id,
        NotificationId = notification.Id,
        TemplateKey = notification.TemplateKey,
        Category = notification.Category,
        Title = notification.Title,
        Body = notification.Body,
        SourceModule = notification.SourceModule,
        SourceEntityType = notification.SourceEntityType,
        SourceEntityId = notification.SourceEntityId,
        Severity = notification.Severity,
        IsRead = recipient.IsRead,
        ReadAt = recipient.ReadAt,
        CreatedAt = notification.CreatedAt,
    };
}
