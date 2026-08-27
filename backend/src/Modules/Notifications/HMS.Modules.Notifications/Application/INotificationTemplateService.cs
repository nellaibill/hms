using HMS.Modules.Notifications.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Notifications.Application;

/// <summary>Public for the same CS0051 reason as <see cref="INotificationService"/> —
/// NotificationTemplatesController's public constructor takes this as a dependency.</summary>
public interface INotificationTemplateService
{
    Task<Result<NotificationTemplateResponse>> CreateAsync(CreateNotificationTemplateRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<NotificationTemplateResponse>> UpdateAsync(Guid id, UpdateNotificationTemplateRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationTemplateResponse>> GetAllAsync(bool? isActive, CancellationToken cancellationToken);
}
