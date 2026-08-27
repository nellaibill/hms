using HMS.Modules.Notifications.Contracts;
using HMS.Modules.Notifications.Domain;

namespace HMS.Modules.Notifications.Application.Mapping;

internal static class NotificationTemplateMappingExtensions
{
    public static NotificationTemplateResponse ToResponse(this NotificationTemplate template) => new()
    {
        Id = template.Id,
        TemplateKey = template.TemplateKey,
        Channel = template.Channel,
        Subject = template.Subject,
        BodyTemplate = template.BodyTemplate,
        IsActive = template.IsActive,
        CreatedAt = template.CreatedAt,
        UpdatedAt = template.UpdatedAt,
    };
}
