using HMS.Modules.Notifications.Contracts;

namespace HMS.Modules.Notifications.Application;

/// <summary>Public for the same CS0051 reason as <see cref="INotificationService"/> —
/// NotificationPreferencesController's public constructor takes this as a dependency.</summary>
public interface INotificationPreferenceService
{
    /// <summary>Only rows the caller has actually saved — a category with no row means "use
    /// the default" (see NotificationPreference's own doc comment), not "explicitly
    /// disabled," so callers should treat a missing category that way rather than reading
    /// its absence from this list as an opt-out.</summary>
    Task<IReadOnlyList<NotificationPreferenceResponse>> GetMyPreferencesAsync(Guid userId, CancellationToken cancellationToken);

    Task<NotificationPreferenceResponse> UpsertMyPreferenceAsync(Guid userId, UpdateNotificationPreferenceRequest request, CancellationToken cancellationToken);
}
