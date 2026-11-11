using HMS.Modules.Sync.Application;
using HMS.Modules.Sync.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Sync.Endpoints;

/// <summary>
/// Business-level cloud sync monitoring for the local (Edge) server's Cloud Sync page
/// (ADR-133 §10). Reads need identity-administration.view; every action needs
/// identity-administration.edit and is written to the Activity Log. Patient name and UHID are
/// included only for callers who also hold patient-management.view. Never returns payloads,
/// field values, the cloud address or any key.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/sync-monitor")]
public class SyncMonitorController : ControllerBase
{
    private const string ViewPermission = "identity-administration.view";
    private const string EditPermission = "identity-administration.edit";
    private const string PatientViewPermission = "patient-management.view";

    private readonly ISyncMonitorService _monitor;

    public SyncMonitorController(ISyncMonitorService monitor)
    {
        _monitor = monitor;
    }

    private bool CanSeePatientDetails => User.HasPermission(PatientViewPermission);

    /// <summary>Overall status: connectivity, pause state, last successful sync and counts.</summary>
    [HttpGet("summary")]
    [RequirePermission(ViewPermission)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken) =>
        Ok(new ApiResponse<SyncSummaryResponse> { Data = await _monitor.GetSummaryAsync(cancellationToken) });

    /// <summary>Patient-level sync status, blocked patients first.</summary>
    [HttpGet("streams")]
    [RequirePermission(ViewPermission)]
    public async Task<IActionResult> GetStreams([FromQuery] SyncStreamListQuery query, CancellationToken cancellationToken)
    {
        var page = await _monitor.GetStreamsAsync(query, CanSeePatientDetails, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<SyncStreamRow>> { Data = page.Items, Meta = Pagination(page) });
    }

    /// <summary>One patient's event history with every send attempt (newest first).</summary>
    [HttpGet("streams/{patientId:guid}/events")]
    [RequirePermission(ViewPermission)]
    public async Task<IActionResult> GetPatientHistory(Guid patientId, CancellationToken cancellationToken) =>
        Ok(new ApiResponse<IReadOnlyList<SyncEventRow>> { Data = await _monitor.GetPatientHistoryAsync(patientId, CanSeePatientDetails, cancellationToken) });

    /// <summary>Events by status; by default the failed ones (Failed, NeedsAttention, Conflict).</summary>
    [HttpGet("events")]
    [RequirePermission(ViewPermission)]
    public async Task<IActionResult> GetEvents([FromQuery] SyncEventListQuery query, CancellationToken cancellationToken)
    {
        var page = await _monitor.GetEventsAsync(query, CanSeePatientDetails, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<SyncEventRow>> { Data = page.Items, Meta = Pagination(page) });
    }

    /// <summary>Retries one failed event (it must be the one blocking its patient).</summary>
    [HttpPost("events/{eventId:guid}/retry")]
    [RequirePermission(EditPermission)]
    public async Task<IActionResult> Retry(Guid eventId, CancellationToken cancellationToken) =>
        ToResponse(await _monitor.RetryEventAsync(eventId, User.GetUserId(), cancellationToken));

    /// <summary>Retries every failed or needs-attention event. Conflicts are excluded.</summary>
    [HttpPost("events/retry-all")]
    [RequirePermission(EditPermission)]
    public async Task<IActionResult> RetryAll(CancellationToken cancellationToken) =>
        ToResponse(await _monitor.RetryAllAsync(User.GetUserId(), cancellationToken));

    /// <summary>Resolves a conflict (LocalWins / CloudWins) or skips an event that needs
    /// attention (Skip).</summary>
    [HttpPost("events/{eventId:guid}/resolve")]
    [RequirePermission(EditPermission)]
    public async Task<IActionResult> Resolve(Guid eventId, [FromBody] SyncResolveRequest request, CancellationToken cancellationToken) =>
        ToResponse(await _monitor.ResolveAsync(eventId, request, User.GetUserId(), cancellationToken));

    /// <summary>Pauses sending for this hospital. Changes keep being saved and queued locally.</summary>
    [HttpPost("pause")]
    [RequirePermission(EditPermission)]
    public async Task<IActionResult> Pause(CancellationToken cancellationToken) =>
        ToResponse(await _monitor.SetPausedAsync(true, User.GetUserId(), cancellationToken));

    [HttpPost("resume")]
    [RequirePermission(EditPermission)]
    public async Task<IActionResult> Resume(CancellationToken cancellationToken) =>
        ToResponse(await _monitor.SetPausedAsync(false, User.GetUserId(), cancellationToken));

    /// <summary>Checks the cloud connection and starts a sync run now.</summary>
    [HttpPost("trigger")]
    [RequirePermission(EditPermission)]
    public async Task<IActionResult> Trigger(CancellationToken cancellationToken) =>
        ToResponse(await _monitor.TriggerAsync(User.GetUserId(), cancellationToken));

    /// <summary>A one-time link (60 s) that opens the Hangfire dashboard for 15 minutes.</summary>
    [HttpPost("dashboard-session")]
    [RequirePermission(EditPermission)]
    public async Task<IActionResult> CreateDashboardSession(CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Forbid();
        }

        return ToResponse(await _monitor.CreateDashboardSessionAsync(userId, cancellationToken));
    }

    private static PaginationMeta Pagination<T>(PagedResult<T> page) => new()
    {
        Page = page.Page,
        PageSize = page.PageSize,
        TotalCount = page.TotalCount,
        TotalPages = page.TotalPages,
    };

    private IActionResult ToResponse<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(new ApiResponse<T> { Data = result.Value });
        }

        var status = result.ErrorCode switch
        {
            SyncMonitorErrorCodes.EventNotFound => StatusCodes.Status404NotFound,
            SyncMonitorErrorCodes.InvalidResolution => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status409Conflict,
        };

        return StatusCode(status, new ApiErrorResponse
        {
            ErrorCode = result.ErrorCode!,
            Message = result.Error!,
            CorrelationId = HttpContext.GetCorrelationId(),
            Timestamp = DateTime.UtcNow,
        });
    }
}
