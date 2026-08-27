using HMS.Modules.Notifications.Contracts;
using HMS.Modules.Notifications.Domain;

namespace HMS.Modules.Notifications.Application.Mapping;

internal static class NotificationPreferenceMappingExtensions
{
    public static NotificationPreferenceResponse ToResponse(this NotificationPreference preference) => new()
    {
        Id = preference.Id,
        Category = preference.Category,
        InAppEnabled = preference.InAppEnabled,
        EmailEnabled = preference.EmailEnabled,
        SmsEnabled = preference.SmsEnabled,
    };
}
