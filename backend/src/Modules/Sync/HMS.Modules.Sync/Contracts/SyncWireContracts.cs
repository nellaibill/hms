using System.Text.Json;

namespace HMS.Modules.Sync.Contracts;

/// <summary>Request body of <c>POST /api/v1/sync/patients</c>: one ordered batch from an Edge
/// server. Events are ordered by stream (patient), then by sequence number.</summary>
public sealed class SyncBatchRequest
{
    /// <summary>Free-form id of the sending server (Sync:NodeId), for diagnostics only.</summary>
    public string SourceNodeId { get; init; } = string.Empty;

    /// <summary>The sender's outbox epoch (random id of its sequence numbering). If a local
    /// database is ever re-created, its numbering restarts under a new epoch and the cloud starts
    /// a fresh baseline for each patient instead of treating the new events as duplicates.</summary>
    public Guid SourceEpoch { get; init; }

    public IReadOnlyList<SyncEventEnvelope> Events { get; init; } = [];
}

/// <summary>One outbox event on the wire.</summary>
public sealed class SyncEventEnvelope
{
    /// <summary>Idempotency key. Re-sending the same EventId never applies it twice.</summary>
    public Guid EventId { get; init; }

    /// <summary>Ordering key: the patient id.</summary>
    public Guid StreamId { get; init; }

    public string AggregateType { get; init; } = string.Empty;
    public Guid AggregateId { get; init; }
    public string OperationType { get; init; } = string.Empty;

    /// <summary>1, 2, 3 ... per stream, with no gaps.</summary>
    public long SequenceNumber { get; init; }

    public int PayloadSchemaVersion { get; init; }

    /// <summary>SHA-256 (hex) of the payload's canonical PostgreSQL jsonb text.</summary>
    public string PayloadHash { get; init; } = string.Empty;

    public DateTime OccurredAt { get; init; }

    /// <summary>The full post-change aggregate snapshot.</summary>
    public JsonElement Payload { get; init; }

    /// <summary>Set only when an admin resolved this event (see <see cref="SyncResolutions"/>).</summary>
    public string? Resolution { get; init; }
}

/// <summary>Response body of <c>POST /api/v1/sync/patients</c>: one result per request event,
/// in request order.</summary>
public sealed class SyncBatchResponse
{
    public IReadOnlyList<SyncEventResult> Results { get; init; } = [];
    public DateTime ServerTime { get; init; }
}

public sealed class SyncEventResult
{
    public Guid EventId { get; init; }

    /// <summary>One of <see cref="SyncResultStatuses"/>.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Cloud-side receipt for an applied event.</summary>
    public Guid? ReceiptId { get; init; }

    public DateTime? ProcessedAt { get; init; }
    public string? ErrorCode { get; init; }

    /// <summary>Human-readable reason. Never contains patient values.</summary>
    public string? Message { get; init; }

    /// <summary>Conflicts only: the cloud's current copy, so the local server can offer
    /// "keep cloud version" without another round trip.</summary>
    public JsonElement? CloudSnapshot { get; init; }
}

/// <summary>Response body of <c>GET /api/v1/sync/ping</c>.</summary>
public sealed class SyncPingResponse
{
    public string HospitalCode { get; init; } = string.Empty;
    public DateTime ServerTime { get; init; }
    public IReadOnlyList<int> SupportedPayloadVersions { get; init; } = [];
}
