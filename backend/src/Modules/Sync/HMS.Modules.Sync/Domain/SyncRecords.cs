namespace HMS.Modules.Sync.Domain;

// Persistence records for the "sync" schema (ADR-133). Most state transitions are single SQL
// statements in Infrastructure (claim, acknowledge, retry), so these types are used for
// mapping, migrations and reads; they carry no behaviour.

/// <summary>sync.outbox_events (Edge): one row per committed aggregate change. Written by
/// <c>OutboxWriter</c> inside the business transaction.</summary>
internal sealed class OutboxEvent
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid StreamId { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public Guid AggregateId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public long SequenceNumber { get; set; }

    /// <summary>Full snapshot (jsonb). Contains patient data: never logged, never returned by
    /// the monitoring API.</summary>
    public string Payload { get; set; } = string.Empty;

    public int PayloadSchemaVersion { get; set; }
    public string PayloadHash { get; set; } = string.Empty;

    /// <summary>Property names only, never values.</summary>
    public string[] ChangedFields { get; set; } = [];

    public string Status { get; set; } = string.Empty;
    public int RetryCount { get; set; }

    public string? Resolution { get; set; }
    public string? ResolutionNote { get; set; }
    public Guid? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public DateTime? SyncedAt { get; set; }

    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public Guid? CloudReceiptId { get; set; }
    public DateTime? CloudProcessedAt { get; set; }

    /// <summary>Conflicts only: the cloud copy returned with the Conflict result (jsonb), used
    /// by the "keep cloud version" resolution. Never returned by the monitoring API.</summary>
    public string? ConflictSnapshot { get; set; }
}

/// <summary>sync.outbox_streams (Edge): one row per aggregate (patient) that has ever had an
/// event. It is the per-patient sequence allocator, the unit a worker claims, and the status
/// row the monitoring table lists.</summary>
internal sealed class OutboxStream
{
    public Guid StreamId { get; set; }
    public string AggregateType { get; set; } = string.Empty;

    /// <summary>Highest sequence number allocated so far.</summary>
    public long LastSequence { get; set; }

    /// <summary>Highest sequence number acknowledged by the cloud.</summary>
    public long LastSyncedSequence { get; set; }

    /// <summary>Synced | Pending | Failed | NeedsAttention | Conflict (the head event's status).</summary>
    public string Status { get; set; } = string.Empty;

    public int RetryCount { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public string? LastErrorCode { get; set; }

    public string? LeaseOwner { get; set; }
    public DateTime? LeaseExpiresAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

/// <summary>sync.outbox_attempts (Edge): append-only record of every send attempt.</summary>
internal sealed class OutboxAttempt
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid StreamId { get; set; }
    public int AttemptNo { get; set; }
    public string WorkerId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }

    /// <summary>A cloud result status (Applied, Rejected, ...) or a transport outcome
    /// (Connectivity, HttpError, AuthFailed).</summary>
    public string Outcome { get; set; } = string.Empty;

    public int? HttpStatus { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int DurationMs { get; set; }
}

/// <summary>sync.sync_state (Edge): exactly one row (Id = 1) per hospital database with the
/// pause switch and the connectivity picture shown on the Cloud Sync page.</summary>
internal sealed class SyncState
{
    public const int SingletonId = 1;

    public int Id { get; set; }

    /// <summary>Random id of this outbox's sequence numbering, created with the row. The cloud
    /// stores it per patient, so if this database is ever re-created and its numbering restarts
    /// at 1, the cloud starts a new baseline instead of mistaking the new events for duplicates.</summary>
    public Guid OutboxEpoch { get; set; }

    public bool IsPaused { get; set; }
    public Guid? PausedBy { get; set; }
    public DateTime? PausedAt { get; set; }

    /// <summary>Set when the cloud rejected this server's key (401/403); cleared by the next
    /// successful probe.</summary>
    public bool AuthFailed { get; set; }

    /// <summary>Null until the first probe has run.</summary>
    public bool? CloudReachable { get; set; }

    public DateTime? LastProbeAt { get; set; }
    public string? LastProbeError { get; set; }
    public int? LastProbeLatencyMs { get; set; }

    public DateTime? LastSuccessfulSyncAt { get; set; }
    public DateTime? LastRunAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

/// <summary>sync.inbox_events (Cloud): the idempotency record of every event the cloud has
/// applied or recorded as skipped. Written in the same transaction as the data change, so
/// "applied" and "acknowledged" can never disagree. Rejected and conflicting events are not
/// stored: a retry after the data is fixed must be evaluated again.</summary>
internal sealed class InboxEvent
{
    public Guid EventId { get; set; }
    public Guid StreamId { get; set; }
    public long SequenceNumber { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;

    /// <summary>Applied | Skipped.</summary>
    public string Status { get; set; } = string.Empty;

    public string? Resolution { get; set; }
    public Guid ReceiptId { get; set; }
    public string? SourceNodeId { get; set; }
    public DateTime ProcessedAt { get; set; }
}

/// <summary>sync.replica_streams (Cloud): per patient, the last sequence applied from the Edge
/// and the version stamp of the cloud copy right after that apply. A different stamp on the
/// next event means the cloud copy was edited outside sync: a conflict.</summary>
internal sealed class ReplicaStream
{
    public Guid StreamId { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public long LastAppliedSequence { get; set; }
    public Guid LastAppliedEventId { get; set; }

    /// <summary>The sender's outbox epoch the sequence numbers belong to (see SyncState.OutboxEpoch).</summary>
    public Guid? SourceEpoch { get; set; }

    /// <summary>SHA-256 over the aggregate's (table, id, xmin) tuples. Null when the aggregate
    /// does not exist in the cloud.</summary>
    public string? AppliedVersionStamp { get; set; }

    public DateTime UpdatedAt { get; set; }
}

/// <summary>sync.conflicts (Cloud): both versions of a conflicting patient, kept for the record
/// whichever way it is resolved.</summary>
internal sealed class SyncConflict
{
    public Guid Id { get; set; }
    public Guid StreamId { get; set; }
    public Guid EventId { get; set; }
    public long SequenceNumber { get; set; }

    /// <summary>jsonb. Patient data: never logged.</summary>
    public string LocalSnapshot { get; set; } = string.Empty;

    /// <summary>jsonb. Null if the cloud copy no longer exists.</summary>
    public string? CloudSnapshot { get; set; }

    public DateTime DetectedAt { get; set; }

    /// <summary>Open | ResolvedLocalWins | ResolvedCloudWins.</summary>
    public string Status { get; set; } = string.Empty;

    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolutionEventId { get; set; }
}

/// <summary>sync_registry.sync_clients (Cloud, hms_platform database): one API key an Edge
/// server uses to push one hospital's data. Only the SHA-256 hash of the key is stored; the key
/// itself is printed once by the <c>sync-key issue</c> command.</summary>
internal sealed class SyncClient
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string HospitalCode { get; set; } = string.Empty;

    /// <summary>First characters of the key, so an operator can tell keys apart.</summary>
    public string KeyPrefix { get; set; } = string.Empty;

    public string KeyHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>sync_registry.tenant_settings (Cloud, hms_platform database): per-hospital sync
/// switches. <see cref="PatientsOwnedByEdge"/> is set at cutover (Decision 1 of ADR-133): from
/// then on the hospital's local server is the system of record for patients, and the cloud
/// refuses patient registration and edits for that hospital.</summary>
internal sealed class TenantSyncSettings
{
    public Guid TenantId { get; set; }
    public string HospitalCode { get; set; } = string.Empty;
    public bool PatientsOwnedByEdge { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
