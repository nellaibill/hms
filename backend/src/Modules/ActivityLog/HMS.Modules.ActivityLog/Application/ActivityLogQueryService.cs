using System.Text.Json;
using HMS.Modules.ActivityLog.Application.Abstractions;
using HMS.Modules.ActivityLog.Contracts;
using HMS.Modules.ActivityLog.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.ActivityLog.Application;

internal class ActivityLogQueryService : IActivityLogQueryService
{
    private readonly IActivityLogRepository _repository;

    public ActivityLogQueryService(IActivityLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResult<ActivityLogResponse>>> GetPagedAsync(ActivityLogListQuery query, CancellationToken cancellationToken)
    {
        if (query.From is not null && query.To is not null && query.From > query.To)
        {
            return Result<PagedResult<ActivityLogResponse>>.Failure(
                ActivityLogErrorCodes.InvalidDateRange,
                "'From' must not be later than 'To'.");
        }

        var page = await _repository.GetPagedAsync(query, cancellationToken);
        var items = page.Items.Select(ToResponse).ToList();

        return Result<PagedResult<ActivityLogResponse>>.Success(new PagedResult<ActivityLogResponse>(items, page.Page, page.PageSize, page.TotalCount));
    }

    public async Task<Result<ActivityLogDetailResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return Result<ActivityLogDetailResponse>.Failure(ActivityLogErrorCodes.NotFound, $"Activity log entry '{id}' was not found.");
        }

        return Result<ActivityLogDetailResponse>.Success(new ActivityLogDetailResponse
        {
            Id = entry.Id,
            TenantId = entry.TenantId,
            UserId = entry.UserId,
            Action = entry.Action,
            Module = entry.Module,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            Description = entry.Description,
            IsSuccess = entry.IsSuccess,
            CreatedAt = entry.CreatedAt,
            OldValues = ParseJson(entry.OldValues),
            NewValues = ParseJson(entry.NewValues),
            IpAddress = entry.IpAddress,
            UserAgent = entry.UserAgent,
            CorrelationId = entry.CorrelationId,
        });
    }

    private static ActivityLogResponse ToResponse(ActivityLogEntry entry) => new()
    {
        Id = entry.Id,
        UserId = entry.UserId,
        Action = entry.Action,
        Module = entry.Module,
        EntityType = entry.EntityType,
        EntityId = entry.EntityId,
        Description = entry.Description,
        IsSuccess = entry.IsSuccess,
        CreatedAt = entry.CreatedAt,
    };

    private static JsonElement? ParseJson(string? json)
        => string.IsNullOrEmpty(json) ? null : JsonDocument.Parse(json).RootElement.Clone();
}
