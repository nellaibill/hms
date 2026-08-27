using HMS.Modules.Notifications.Application.Abstractions;
using HMS.Modules.Notifications.Application.Mapping;
using HMS.Modules.Notifications.Contracts;
using HMS.Modules.Notifications.Domain;

namespace HMS.Modules.Notifications.Application;

internal class NotificationPreferenceService : INotificationPreferenceService
{
    private readonly INotificationPreferenceRepository _repository;

    public NotificationPreferenceService(INotificationPreferenceRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<NotificationPreferenceResponse>> GetMyPreferencesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var preferences = await _repository.GetByUserAsync(userId, cancellationToken);
        return preferences.Select(p => p.ToResponse()).ToList();
    }

    public async Task<NotificationPreferenceResponse> UpsertMyPreferenceAsync(Guid userId, UpdateNotificationPreferenceRequest request, CancellationToken cancellationToken)
    {
        var preference = await _repository.GetByUserAndCategoryAsync(userId, request.Category, cancellationToken);
        if (preference is null)
        {
            preference = NotificationPreference.Create(userId, request.Category, userId);
            preference.UpdateChannels(request.InAppEnabled, request.EmailEnabled, request.SmsEnabled, userId);
            await _repository.AddAsync(preference, cancellationToken);
        }
        else
        {
            preference.UpdateChannels(request.InAppEnabled, request.EmailEnabled, request.SmsEnabled, userId);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return preference.ToResponse();
    }
}
