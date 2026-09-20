using HMS.Modules.ActivityLog.Application.Abstractions;
using HMS.Modules.ActivityLog.Contracts;
using HMS.Modules.ActivityLog.Domain;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.ActivityLog.Application;

internal class ActivityLogService : IActivityLogService
{
    private readonly IActivityLogRepository _repository;
    private readonly IActivityLogContextProvider _contextProvider;
    private readonly ILogger<ActivityLogService> _logger;

    public ActivityLogService(
        IActivityLogRepository repository,
        IActivityLogContextProvider contextProvider,
        ILogger<ActivityLogService> logger)
    {
        _repository = repository;
        _contextProvider = contextProvider;
        _logger = logger;
    }

    public async Task LogAsync(ActivityLogRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var context = _contextProvider.GetCurrent();

            var entry = ActivityLogEntry.Create(
                context.TenantId,
                request.UserId ?? context.UserId,
                request.Action,
                request.Module,
                request.EntityType,
                request.EntityId,
                request.Description,
                ActivityLogSanitizer.Sanitize(request.OldValues),
                ActivityLogSanitizer.Sanitize(request.NewValues),
                context.IpAddress,
                context.UserAgent,
                context.CorrelationId,
                request.IsSuccess);

            await _repository.AddAsync(entry, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Deliberately swallowed — see IActivityLogService's doc comment.
            _logger.LogWarning(ex, "ActivityLog: failed to record {Action} on {Module}/{EntityType} {EntityId}",
                request.Action, request.Module, request.EntityType, request.EntityId);
        }
    }
}
