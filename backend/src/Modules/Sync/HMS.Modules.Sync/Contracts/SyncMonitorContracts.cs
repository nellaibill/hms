using HMS.Shared.Kernel;

namespace HMS.Modules.Sync.Contracts;

/// <summary>Overall states the Cloud Sync page shows as its headline.</summary>
public static class SyncOverallStatuses
{
    /// <summary>Sync:Role is not Edge: nothing is captured or sent from this server.</summary>
    public const string NotEnabled = "NotEnabled";

    /// <summary>Edge, but no cloud key is configured for this hospital.</summary>
    public const string NotConfigured = "NotConfigured";

    /// <summary>Sending switched off in configuration (Sync:Enabled = false).</summary>
    public const string Disabled = "Disabled";

    public const string Paused = "Paused";

    /// <summary>The cloud rejected this server's key.</summary>
    public const string AuthFailed = "AuthFailed";

    /// <summary>The cloud cannot be reached; changes are queued locally.</summary>
    public const string Offline = "Offline";

    /// <summary>Some patients are blocked (needs attention or conflict).</summary>
    public const string Attention = "Attention";

    /// <summary>Changes are waiting or being sent.</summary>
    public const string Syncing = "Syncing";

    /// <summary>Everything is synced.</summary>
    public const string Healthy = "Healthy";
}

/// <summary>GET /api/v1/sync-monitor/summary. Status values only: no cloud address, no key.</summary>
public sealed class SyncSummaryResponse
{
    public string Role { get; init; } = string.Empty;
    public string OverallStatus { get; init; } = string.Empty;
    public bool CaptureEnabled { get; init; }
    public bool SendingEnabled { get; init; }
    public bool HasCloudKey { get; init; }
    public int IntervalInMinutes { get; init; }
    public bool DashboardAvailable { get; init; }

    public bool IsPaused { get; init; }
    public DateTime? PausedAt { get; init; }
    public bool AuthFailed { get; init; }
    public bool? CloudReachable { get; init; }
    public DateTime? LastProbeAt { get; init; }
    public string? LastProbeError { get; init; }
    public int? LastProbeLatencyMs { get; init; }
    public DateTime? LastSuccessfulSyncAt { get; init; }
    public DateTime? LastRunAt { get; init; }

    /// <summary>True when the local database answered this request.</summary>
    public bool LocalDatabaseAvailable { get; init; }

    public int PendingEvents { get; init; }
    public int ProcessingEvents { get; init; }
    public int FailedEvents { get; init; }
    public int NeedsAttentionEvents { get; init; }
    public int ConflictEvents { get; init; }

    /// <summary>Events acknowledged by the cloud in the last 24 hours.</summary>
    public int SyncedLast24Hours { get; init; }

    public int PatientsWithPendingChanges { get; init; }
    public int BlockedPatients { get; init; }
    public DateTime? OldestPendingAt { get; init; }
}

/// <summary>GET /api/v1/sync-monitor/streams query.</summary>
public sealed class SyncStreamListQuery : PagedRequest
{
    /// <summary>One of the stream statuses (Synced, Pending, Failed, NeedsAttention, Conflict),
    /// or "Unsynced" for anything not yet synced.</summary>
    public string? Status { get; set; }
}

/// <summary>One patient in the monitoring table. Name and UHID are filled only when the caller
/// also holds patient-management.view.</summary>
public sealed class SyncStreamRow
{
    public Guid PatientId { get; init; }
    public string? Uhid { get; init; }
    public string? PatientName { get; init; }
    public bool? PatientDeleted { get; init; }
    public string Status { get; init; } = string.Empty;
    public long PendingEvents { get; init; }
    public long LastSequence { get; init; }
    public long LastSyncedSequence { get; init; }
    public int RetryCount { get; init; }
    public DateTime? LastAttemptAt { get; init; }
    public DateTime? NextAttemptAt { get; init; }
    public string? LastErrorCode { get; init; }
    public string? LastErrorMessage { get; init; }
    public Guid? HeadEventId { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>One outbox event. Lists the names of the fields that changed, never their values.</summary>
public sealed class SyncEventRow
{
    public Guid EventId { get; init; }
    public Guid PatientId { get; init; }
    public string? Uhid { get; init; }
    public string? PatientName { get; init; }
    public long SequenceNumber { get; init; }
    public string OperationType { get; init; } = string.Empty;
    public IReadOnlyList<string> ChangedFields { get; init; } = [];
    public string Status { get; init; } = string.Empty;
    public int RetryCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastAttemptAt { get; init; }
    public DateTime? NextRetryAt { get; init; }
    public DateTime? SyncedAt { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Resolution { get; init; }
    public DateTime? ResolvedAt { get; init; }

    /// <summary>Conflicts only: whether the cloud's version was received, so "keep cloud
    /// version" is possible.</summary>
    public bool HasCloudVersion { get; init; }

    /// <summary>Filled by the per-patient history endpoint only.</summary>
    public IReadOnlyList<SyncAttemptRow>? Attempts { get; init; }
}

public sealed class SyncAttemptRow
{
    public int AttemptNo { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime FinishedAt { get; init; }
    public string Outcome { get; init; } = string.Empty;
    public int? HttpStatus { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public int DurationMs { get; init; }
}

/// <summary>GET /api/v1/sync-monitor/events query.</summary>
public sealed class SyncEventListQuery : PagedRequest
{
    /// <summary>Comma-separated statuses; defaults to Failed, NeedsAttention and Conflict.</summary>
    public string? Status { get; set; }
}

/// <summary>POST /api/v1/sync-monitor/events/{eventId}/resolve.</summary>
public sealed class SyncResolveRequest
{
    /// <summary>LocalWins | CloudWins (conflicts) or Skip (needs attention).</summary>
    public string Resolution { get; init; } = string.Empty;

    /// <summary>Why. Recorded in the audit log.</summary>
    public string? Note { get; init; }
}

public sealed class SyncActionResponse
{
    public int Affected { get; init; }
    public string Message { get; init; } = string.Empty;
}

/// <summary>POST /api/v1/sync-monitor/dashboard-session: a one-time link that opens the Hangfire
/// dashboard in a new tab.</summary>
public sealed class SyncDashboardSessionResponse
{
    /// <summary>Server-relative URL; valid once, for 60 seconds.</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>When the dashboard session itself ends.</summary>
    public DateTime SessionExpiresAt { get; init; }
}
