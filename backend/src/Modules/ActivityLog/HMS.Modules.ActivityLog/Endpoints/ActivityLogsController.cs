using HMS.Modules.ActivityLog.Application;
using HMS.Modules.ActivityLog.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.ActivityLog.Endpoints;

/// <summary>
/// Read-only audit trail. Gated by "identity-administration.view" — the same admin-only
/// permission category that guards user and role management — and by the tenant's
/// "activity-log" feature. There is deliberately no POST/PUT/PATCH/DELETE action: audit
/// rows are only ever written in-process through IActivityLogService.
/// </summary>
[ApiController]
[RequireFeature("activity-log")]
[Route("api/v1/activity-logs")]
public class ActivityLogsController : ControllerBase
{
    private readonly IActivityLogQueryService _queryService;

    public ActivityLogsController(IActivityLogQueryService queryService)
    {
        _queryService = queryService;
    }

    /// <summary>Paged audit entries, newest first. Filter by date range (from/to), user,
    /// module, action, entity type, entity id; "search" matches description, entity type
    /// and entity id.</summary>
    /// <response code="200">A page of entries (without old/new values).</response>
    /// <response code="400">The date range is invalid.</response>
    [Authorize]
    [RequirePermission("identity-administration.view")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ActivityLogListQuery query, CancellationToken cancellationToken)
    {
        var result = await _queryService.GetPagedAsync(query, cancellationToken);
        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        var meta = new PaginationMeta
        {
            Page = result.Value!.Page,
            PageSize = result.Value.PageSize,
            TotalCount = result.Value.TotalCount,
            TotalPages = result.Value.TotalPages,
        };

        return Ok(new ApiResponse<IReadOnlyList<ActivityLogResponse>> { Data = result.Value.Items, Meta = meta });
    }

    /// <summary>One entry in full, including old/new values, IP address, user agent and
    /// correlation id.</summary>
    /// <response code="200">The entry.</response>
    /// <response code="404">No such entry.</response>
    [Authorize]
    [RequirePermission("identity-administration.view")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _queryService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<ActivityLogDetailResponse> { Data = result.Value })
            : MapFailure(result.ErrorCode!, result.Error!);
    }

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            ActivityLogErrorCodes.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest,
        };

        var error = new ApiErrorResponse
        {
            ErrorCode = errorCode,
            Message = message,
            CorrelationId = HttpContext.GetCorrelationId(),
            Timestamp = DateTime.UtcNow,
        };

        return StatusCode(status, error);
    }
}
