using Hangfire;
using HMS.Modules.ActivityLog.Application;
using HMS.Modules.ActivityLog.Contracts;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Sync.Contracts;
using HMS.Modules.Sync.Domain;
using HMS.Modules.Sync.Infrastructure;
using HMS.Modules.Sync.Infrastructure.Jobs;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HMS.Modules.Sync.Application;

/// <summary>Error codes of the monitoring API.</summary>
public static class SyncMonitorErrorCodes
{
    public const string NotEdge = "SYNC.NOT_EDGE_SERVER";
    public const string EventNotFound = "SYNC.EVENT_NOT_FOUND";
    public const string NotRetryable = "SYNC.EVENT_NOT_RETRYABLE";
    public const string InvalidResolution = "SYNC.INVALID_RESOLUTION";
    public const string CloudVersionMissing = "SYNC.CLOUD_VERSION_MISSING";
    public const string ApplyFailed = "SYNC.LOCAL_APPLY_FAILED";
    public const string NotConfigured = "SYNC.NOT_CONFIGURED";
}

/// <summary>Business-level sync monitoring for the Cloud Sync page (ADR-133 §10). Public for the
/// CS0051 reason: SyncMonitorController depends on it. The React page only ever talks to this
/// service; it never reaches the outbox tables or the cloud directly.</summary>
public interface ISyncMonitorService
{
    Task<SyncSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken);

    Task<PagedResult<SyncStreamRow>> GetStreamsAsync(SyncStreamListQuery query, bool includePatientDetails, CancellationToken cancellationToken);

    Task<IReadOnlyList<SyncEventRow>> GetPatientHistoryAsync(Guid patientId, bool includePatientDetails, CancellationToken cancellationToken);

    Task<PagedResult<SyncEventRow>> GetEventsAsync(SyncEventListQuery query, bool includePatientDetails, CancellationToken cancellationToken);

    Task<Result<SyncActionResponse>> RetryEventAsync(Guid eventId, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<SyncActionResponse>> RetryAllAsync(Guid? actorId, CancellationToken cancellationToken);

    Task<Result<SyncActionResponse>> ResolveAsync(Guid eventId, SyncResolveRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<SyncActionResponse>> SetPausedAsync(bool paused, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<SyncActionResponse>> TriggerAsync(Guid? actorId, CancellationToken cancellationToken);

    Task<Result<SyncDashboardSessionResponse>> CreateDashboardSessionAsync(Guid userId, CancellationToken cancellationToken);
}

/// <summary>Every action is authorized by the controller and written to the Activity Log here.
/// Audit entries carry event and patient ids only, never patient data.</summary>
internal sealed class SyncMonitorService : ISyncMonitorService
{
    private const string AuditModule = "Sync";
    private const int HistoryLimit = 200;

    private readonly SyncDbContext _dbContext;
    private readonly OutboxStore _store;
    private readonly IPatientSyncService _patientSync;
    private readonly IActivityLogService _activityLog;
    private readonly ITenantContext _tenantContext;
    private readonly IServiceProvider _services;
    private readonly SyncOptions _options;
    private readonly ILogger<SyncMonitorService> _logger;

    public SyncMonitorService(
        SyncDbContext dbContext,
        OutboxStore store,
        IPatientSyncService patientSync,
        IActivityLogService activityLog,
        ITenantContext tenantContext,
        IServiceProvider services,
        IOptions<SyncOptions> options,
        ILogger<SyncMonitorService> logger)
    {
        _dbContext = dbContext;
        _store = store;
        _patientSync = patientSync;
        _activityLog = activityLog;
        _tenantContext = tenantContext;
        _services = services;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SyncSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        if (!_options.IsEdge)
        {
            return new SyncSummaryResponse
            {
                Role = _options.Role.ToString(),
                OverallStatus = SyncOverallStatuses.NotEnabled,
                IntervalInMinutes = _options.IntervalInMinutes,
                LocalDatabaseAvailable = true,
            };
        }

        SyncState state;
        try
        {
            state = await _store.GetStateAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Sync summary: the local database did not answer.");
            return new SyncSummaryResponse
            {
                Role = _options.Role.ToString(),
                OverallStatus = SyncOverallStatuses.Attention,
                CaptureEnabled = true,
                LocalDatabaseAvailable = false,
            };
        }

        var counts = await _dbContext.OutboxEvents
            .AsNoTracking()
            .Where(e => e.Status != SyncEventStatuses.Synced)
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count, cancellationToken);

        var since = DateTime.UtcNow.AddHours(-24);
        var syncedLastDay = await _dbContext.OutboxEvents.AsNoTracking()
            .CountAsync(e => e.Status == SyncEventStatuses.Synced && e.SyncedAt >= since, cancellationToken);
        var pendingPatients = await _dbContext.OutboxStreams.AsNoTracking()
            .CountAsync(s => s.LastSyncedSequence < s.LastSequence, cancellationToken);
        var blockedPatients = await _dbContext.OutboxStreams.AsNoTracking()
            .CountAsync(s => s.Status == SyncEventStatuses.NeedsAttention || s.Status == SyncEventStatuses.Conflict, cancellationToken);
        var oldestPending = await _dbContext.OutboxEvents.AsNoTracking()
            .Where(e => e.Status != SyncEventStatuses.Synced)
            .MinAsync(e => (DateTime?)e.CreatedAt, cancellationToken);

        int Count(string status) => counts.TryGetValue(status, out var count) ? count : 0;

        var hasKey = await HasCloudKeyAsync(cancellationToken);
        var overall =
            !hasKey ? SyncOverallStatuses.NotConfigured
            : !_options.Enabled ? SyncOverallStatuses.Disabled
            : state.IsPaused ? SyncOverallStatuses.Paused
            : state.AuthFailed ? SyncOverallStatuses.AuthFailed
            : state.CloudReachable == false ? SyncOverallStatuses.Offline
            : blockedPatients > 0 ? SyncOverallStatuses.Attention
            : pendingPatients > 0 ? SyncOverallStatuses.Syncing
            : SyncOverallStatuses.Healthy;

        return new SyncSummaryResponse
        {
            Role = _options.Role.ToString(),
            OverallStatus = overall,
            CaptureEnabled = true,
            SendingEnabled = _options.Enabled,
            HasCloudKey = hasKey,
            IntervalInMinutes = _options.IntervalInMinutes,
            DashboardAvailable = true,
            IsPaused = state.IsPaused,
            PausedAt = state.PausedAt,
            AuthFailed = state.AuthFailed,
            CloudReachable = state.CloudReachable,
            LastProbeAt = state.LastProbeAt,
            LastProbeError = state.LastProbeError,
            LastProbeLatencyMs = state.LastProbeLatencyMs,
            LastSuccessfulSyncAt = state.LastSuccessfulSyncAt,
            LastRunAt = state.LastRunAt,
            LocalDatabaseAvailable = true,
            PendingEvents = Count(SyncEventStatuses.Pending),
            ProcessingEvents = Count(SyncEventStatuses.Processing),
            FailedEvents = Count(SyncEventStatuses.Failed),
            NeedsAttentionEvents = Count(SyncEventStatuses.NeedsAttention),
            ConflictEvents = Count(SyncEventStatuses.Conflict),
            SyncedLast24Hours = syncedLastDay,
            PatientsWithPendingChanges = pendingPatients,
            BlockedPatients = blockedPatients,
            OldestPendingAt = oldestPending,
        };
    }

    public async Task<PagedResult<SyncStreamRow>> GetStreamsAsync(SyncStreamListQuery query, bool includePatientDetails, CancellationToken cancellationToken)
    {
        if (!_options.IsEdge)
        {
            return new PagedResult<SyncStreamRow>([], query.Page, query.PageSize, 0);
        }

        var streams = _dbContext.OutboxStreams.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            streams = query.Status == "Unsynced"
                ? streams.Where(s => s.LastSyncedSequence < s.LastSequence)
                : streams.Where(s => s.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            if (Guid.TryParse(term, out var patientId))
            {
                streams = streams.Where(s => s.StreamId == patientId);
            }
            else if (includePatientDetails)
            {
                // Name/UHID search only for callers allowed to see patient details.
                var ids = await _patientSync.SearchPatientIdsAsync(term, 500, cancellationToken);
                streams = streams.Where(s => ids.Contains(s.StreamId));
            }
            else
            {
                streams = streams.Where(_ => false);
            }
        }

        var total = await streams.CountAsync(cancellationToken);
        var page = await streams
            .OrderBy(s => s.Status == SyncEventStatuses.Conflict ? 0
                : s.Status == SyncEventStatuses.NeedsAttention ? 1
                : s.Status == SyncEventStatuses.Failed ? 2
                : s.Status == SyncEventStatuses.Pending ? 3
                : 4)
            .ThenByDescending(s => s.UpdatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var pageIds = page.Select(s => s.StreamId).ToList();
        var heads = await _dbContext.OutboxEvents
            .AsNoTracking()
            .Where(e => pageIds.Contains(e.StreamId)
                && _dbContext.OutboxStreams.Any(s => s.StreamId == e.StreamId && s.LastSyncedSequence + 1 == e.SequenceNumber))
            .Select(e => new { e.StreamId, e.EventId, e.ErrorMessage })
            .ToDictionaryAsync(e => e.StreamId, cancellationToken);

        var patients = includePatientDetails
            ? await _patientSync.GetDisplayInfoAsync(pageIds, cancellationToken)
            : new Dictionary<Guid, PatientSyncDisplay>();

        var rows = page.Select(s =>
        {
            patients.TryGetValue(s.StreamId, out var patient);
            heads.TryGetValue(s.StreamId, out var head);
            return new SyncStreamRow
            {
                PatientId = s.StreamId,
                Uhid = patient?.Uhid,
                PatientName = patient?.FullName,
                PatientDeleted = patient?.IsDeleted,
                Status = s.Status,
                PendingEvents = s.LastSequence - s.LastSyncedSequence,
                LastSequence = s.LastSequence,
                LastSyncedSequence = s.LastSyncedSequence,
                RetryCount = s.RetryCount,
                LastAttemptAt = s.LastAttemptAt,
                NextAttemptAt = s.NextAttemptAt,
                LastErrorCode = s.LastErrorCode,
                LastErrorMessage = head?.ErrorMessage,
                HeadEventId = head?.EventId,
                UpdatedAt = s.UpdatedAt,
            };
        }).ToList();

        return new PagedResult<SyncStreamRow>(rows, query.Page, query.PageSize, total);
    }

    public async Task<IReadOnlyList<SyncEventRow>> GetPatientHistoryAsync(Guid patientId, bool includePatientDetails, CancellationToken cancellationToken)
    {
        if (!_options.IsEdge)
        {
            return [];
        }

        var events = await _dbContext.OutboxEvents
            .AsNoTracking()
            .Where(e => e.StreamId == patientId)
            .OrderByDescending(e => e.SequenceNumber)
            .Take(HistoryLimit)
            .ToListAsync(cancellationToken);

        var eventIds = events.Select(e => e.EventId).ToList();
        var attempts = (await _dbContext.OutboxAttempts
                .AsNoTracking()
                .Where(a => eventIds.Contains(a.EventId))
                .OrderBy(a => a.AttemptNo)
                .ToListAsync(cancellationToken))
            .ToLookup(a => a.EventId);

        var patients = includePatientDetails
            ? await _patientSync.GetDisplayInfoAsync([patientId], cancellationToken)
            : new Dictionary<Guid, PatientSyncDisplay>();

        return events.Select(e => ToRow(e, patients, attempts[e.EventId].Select(ToRow).ToList())).ToList();
    }

    public async Task<PagedResult<SyncEventRow>> GetEventsAsync(SyncEventListQuery query, bool includePatientDetails, CancellationToken cancellationToken)
    {
        if (!_options.IsEdge)
        {
            return new PagedResult<SyncEventRow>([], query.Page, query.PageSize, 0);
        }

        var statuses = string.IsNullOrWhiteSpace(query.Status)
            ? [SyncEventStatuses.Failed, SyncEventStatuses.NeedsAttention, SyncEventStatuses.Conflict]
            : query.Status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(SyncEventStatuses.All.Contains)
                .ToArray();

        var events = _dbContext.OutboxEvents.AsNoTracking().Where(e => statuses.Contains(e.Status));
        var total = await events.CountAsync(cancellationToken);
        var page = await events
            .OrderBy(e => e.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var patients = includePatientDetails
            ? await _patientSync.GetDisplayInfoAsync(page.Select(e => e.StreamId).Distinct().ToList(), cancellationToken)
            : new Dictionary<Guid, PatientSyncDisplay>();

        return new PagedResult<SyncEventRow>(page.Select(e => ToRow(e, patients, null)).ToList(), query.Page, query.PageSize, total);
    }

    public async Task<Result<SyncActionResponse>> RetryEventAsync(Guid eventId, Guid? actorId, CancellationToken cancellationToken)
    {
        if (!_options.IsEdge)
        {
            return NotEdge<SyncActionResponse>();
        }

        var outboxEvent = await _dbContext.OutboxEvents.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (outboxEvent is null)
        {
            return Result<SyncActionResponse>.Failure(SyncMonitorErrorCodes.EventNotFound, "No such sync event.");
        }

        if (outboxEvent.Status is not (SyncEventStatuses.Failed or SyncEventStatuses.NeedsAttention))
        {
            return Result<SyncActionResponse>.Failure(SyncMonitorErrorCodes.NotRetryable, $"Only failed events can be retried (this one is {outboxEvent.Status}). Conflicts are resolved, not retried.");
        }

        var reset = await _store.ResetForRetryAsync([outboxEvent.StreamId], cancellationToken);
        if (!reset.Contains(eventId))
        {
            return Result<SyncActionResponse>.Failure(SyncMonitorErrorCodes.NotRetryable, "This event is not the one blocking its patient; retry that one instead.");
        }

        await AuditAsync("Retry", "SyncEvent", eventId.ToString(), $"Retried sync event {outboxEvent.SequenceNumber} of patient {outboxEvent.StreamId}.", actorId, cancellationToken);
        return Result<SyncActionResponse>.Success(new SyncActionResponse { Affected = 1, Message = "The event will be sent on the next sync run." });
    }

    public async Task<Result<SyncActionResponse>> RetryAllAsync(Guid? actorId, CancellationToken cancellationToken)
    {
        if (!_options.IsEdge)
        {
            return NotEdge<SyncActionResponse>();
        }

        var reset = await _store.ResetForRetryAsync(null, cancellationToken);
        await AuditAsync("RetryAll", "SyncState", null, $"Retried all {reset.Count} failed sync event(s).", actorId, cancellationToken);
        return Result<SyncActionResponse>.Success(new SyncActionResponse
        {
            Affected = reset.Count,
            Message = reset.Count == 0 ? "Nothing to retry." : $"{reset.Count} event(s) will be sent on the next sync run.",
        });
    }

    public async Task<Result<SyncActionResponse>> ResolveAsync(Guid eventId, SyncResolveRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (!_options.IsEdge)
        {
            return NotEdge<SyncActionResponse>();
        }

        if (!SyncResolutions.All.Contains(request.Resolution))
        {
            return Result<SyncActionResponse>.Failure(SyncMonitorErrorCodes.InvalidResolution, "Resolution must be LocalWins, CloudWins or Skip.");
        }

        var outboxEvent = await _dbContext.OutboxEvents.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (outboxEvent is null)
        {
            return Result<SyncActionResponse>.Failure(SyncMonitorErrorCodes.EventNotFound, "No such sync event.");
        }

        var expectedStatus = request.Resolution == SyncResolutions.Skip ? SyncEventStatuses.NeedsAttention : SyncEventStatuses.Conflict;
        if (outboxEvent.Status != expectedStatus)
        {
            return Result<SyncActionResponse>.Failure(
                SyncMonitorErrorCodes.InvalidResolution,
                request.Resolution == SyncResolutions.Skip
                    ? "Only an event that needs attention can be skipped."
                    : "Only a conflicting event can be resolved this way.");
        }

        if (request.Resolution == SyncResolutions.CloudWins)
        {
            var cloudSnapshot = await _store.GetConflictSnapshotAsync(eventId, cancellationToken);
            if (cloudSnapshot is null)
            {
                return Result<SyncActionResponse>.Failure(SyncMonitorErrorCodes.CloudVersionMissing, "The cloud's version was not received with this conflict. Send the local version, or retry later.");
            }

            // Overwrites the local patient with the cloud's copy through the normal (captured)
            // save, so a new event carrying the cloud's version queues up behind this one.
            var applied = await _patientSync.ApplySnapshotLocallyAsync(cloudSnapshot, cancellationToken);
            if (!applied.Succeeded)
            {
                return Result<SyncActionResponse>.Failure(SyncMonitorErrorCodes.ApplyFailed, $"The cloud's version could not be applied here: {applied.Message}");
            }
        }

        var marked = await _store.MarkResolutionAsync(eventId, request.Resolution, expectedStatus, actorId, request.Note, cancellationToken);
        if (!marked)
        {
            return Result<SyncActionResponse>.Failure(SyncMonitorErrorCodes.NotRetryable, "This event changed meanwhile. Refresh and try again.");
        }

        await AuditAsync(
            "Resolve",
            "SyncEvent",
            eventId.ToString(),
            $"Resolved sync event {outboxEvent.SequenceNumber} of patient {outboxEvent.StreamId} as {request.Resolution}.",
            actorId,
            cancellationToken,
            newValues: new { request.Resolution, request.Note });

        return Result<SyncActionResponse>.Success(new SyncActionResponse
        {
            Affected = 1,
            Message = request.Resolution switch
            {
                SyncResolutions.LocalWins => "The local version will overwrite the cloud copy on the next sync run.",
                SyncResolutions.CloudWins => "The cloud's version was copied here; both sides will match after the next sync run.",
                _ => "The event was skipped. The patient's next change will carry the full record.",
            },
        });
    }

    public async Task<Result<SyncActionResponse>> SetPausedAsync(bool paused, Guid? actorId, CancellationToken cancellationToken)
    {
        if (!_options.IsEdge)
        {
            return NotEdge<SyncActionResponse>();
        }

        await _store.GetStateAsync(cancellationToken);
        await _store.SetPausedAsync(paused, actorId, cancellationToken);
        await AuditAsync(paused ? "Pause" : "Resume", "SyncState", null, paused ? "Paused cloud sync." : "Resumed cloud sync.", actorId, cancellationToken);
        return Result<SyncActionResponse>.Success(new SyncActionResponse
        {
            Affected = 1,
            Message = paused ? "Sync paused. Changes keep being saved locally and are sent after resuming." : "Sync resumed.",
        });
    }

    public async Task<Result<SyncActionResponse>> TriggerAsync(Guid? actorId, CancellationToken cancellationToken)
    {
        if (!_options.IsEdge)
        {
            return NotEdge<SyncActionResponse>();
        }

        if (!await HasCloudKeyAsync(cancellationToken) || _tenantContext.TenantId is not { } tenantId)
        {
            return Result<SyncActionResponse>.Failure(SyncMonitorErrorCodes.NotConfigured, "No cloud key is configured for this hospital on this server.");
        }

        // Probe first, so "Sync now" also works straight after the internet comes back.
        var jobs = _services.GetRequiredService<IBackgroundJobClient>();
        var probeJobId = jobs.Enqueue<SyncProbeJob>(job => job.RunAsync(CancellationToken.None));
        jobs.ContinueJobWith<SyncTenantJob>(probeJobId, job => job.RunAsync(tenantId, CancellationToken.None));

        await AuditAsync("Trigger", "SyncState", null, "Started a cloud sync run.", actorId, cancellationToken);
        return Result<SyncActionResponse>.Success(new SyncActionResponse { Affected = 1, Message = "Sync started." });
    }

    public async Task<Result<SyncDashboardSessionResponse>> CreateDashboardSessionAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!_options.IsEdge || _tenantContext.TenantId is not { } tenantId)
        {
            return NotEdge<SyncDashboardSessionResponse>();
        }

        var access = _services.GetRequiredService<HangfireDashboardAccess>();
        var (url, expiresAt) = access.CreateHandoff(userId, tenantId);
        await AuditAsync("OpenDashboard", "SyncState", null, "Opened the Hangfire job dashboard.", userId, cancellationToken);
        return Result<SyncDashboardSessionResponse>.Success(new SyncDashboardSessionResponse { Url = url, SessionExpiresAt = expiresAt });
    }

    private static SyncEventRow ToRow(OutboxEvent e, IReadOnlyDictionary<Guid, PatientSyncDisplay> patients, IReadOnlyList<SyncAttemptRow>? attempts)
    {
        patients.TryGetValue(e.StreamId, out var patient);
        return new SyncEventRow
        {
            EventId = e.EventId,
            PatientId = e.StreamId,
            Uhid = patient?.Uhid,
            PatientName = patient?.FullName,
            SequenceNumber = e.SequenceNumber,
            OperationType = e.OperationType,
            ChangedFields = e.ChangedFields,
            Status = e.Status,
            RetryCount = e.RetryCount,
            CreatedAt = e.CreatedAt,
            LastAttemptAt = e.LastAttemptAt,
            NextRetryAt = e.NextRetryAt,
            SyncedAt = e.SyncedAt,
            ErrorCode = e.ErrorCode,
            ErrorMessage = e.ErrorMessage,
            Resolution = e.Resolution,
            ResolvedAt = e.ResolvedAt,
            HasCloudVersion = e.ConflictSnapshot is not null,
            Attempts = attempts,
        };
    }

    private static SyncAttemptRow ToRow(OutboxAttempt a) => new()
    {
        AttemptNo = a.AttemptNo,
        StartedAt = a.StartedAt,
        FinishedAt = a.FinishedAt,
        Outcome = a.Outcome,
        HttpStatus = a.HttpStatus,
        ErrorCode = a.ErrorCode,
        ErrorMessage = a.ErrorMessage,
        DurationMs = a.DurationMs,
    };

    private Task AuditAsync(string action, string entityType, string? entityId, string description, Guid? actorId, CancellationToken cancellationToken, object? newValues = null) =>
        _activityLog.LogAsync(
            new ActivityLogRequest
            {
                Action = action,
                Module = AuditModule,
                EntityType = entityType,
                EntityId = entityId,
                Description = description,
                NewValues = newValues,
                UserId = actorId,
            },
            cancellationToken);

    private async Task<bool> HasCloudKeyAsync(CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is not { } tenantId)
        {
            return false;
        }

        var tenant = await _services.GetRequiredService<ITenantDirectory>().FindByIdAsync(tenantId, cancellationToken);
        return tenant is not null && _options.FindApiKey(tenant.HospitalCode) is not null;
    }

    private static Result<T> NotEdge<T>() =>
        Result<T>.Failure(SyncMonitorErrorCodes.NotEdge, "Cloud sync is not enabled on this server (Sync:Role is not Edge).");
}
