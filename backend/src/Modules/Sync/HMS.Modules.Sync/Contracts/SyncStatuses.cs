namespace HMS.Modules.Sync.Contracts;

/// <summary>Lifecycle of one local outbox event (sync.outbox_events.status).</summary>
public static class SyncEventStatuses
{
    /// <summary>Waiting to be sent (new, or behind a blocked event of the same patient).</summary>
    public const string Pending = "Pending";

    /// <summary>Claimed by a worker and in flight to the cloud.</summary>
    public const string Processing = "Processing";

    /// <summary>Acknowledged by the cloud.</summary>
    public const string Synced = "Synced";

    /// <summary>Transient failure; an automatic retry is scheduled (next_retry_at).</summary>
    public const string Failed = "Failed";

    /// <summary>Permanent error or retries exhausted. Blocks this patient until an admin
    /// retries or skips it.</summary>
    public const string NeedsAttention = "NeedsAttention";

    /// <summary>The cloud copy was changed outside sync. Blocks this patient until an admin
    /// resolves it.</summary>
    public const string Conflict = "Conflict";

    public static readonly IReadOnlyList<string> All = [Pending, Processing, Synced, Failed, NeedsAttention, Conflict];

    /// <summary>Statuses that stop a patient's stream until someone acts.</summary>
    public static readonly IReadOnlyList<string> Blocking = [NeedsAttention, Conflict];
}

/// <summary>Per-event outcome the cloud returns for a sync batch.</summary>
public static class SyncResultStatuses
{
    public const string Applied = "Applied";

    /// <summary>Already applied earlier (same EventId, or a sequence number already passed).
    /// Treated exactly like <see cref="Applied"/> by the sender.</summary>
    public const string Duplicate = "Duplicate";

    /// <summary>A resolution marker (Skip / CloudWins) was recorded; nothing was applied.</summary>
    public const string Skipped = "Skipped";

    /// <summary>Permanent validation failure. Not retried automatically.</summary>
    public const string Rejected = "Rejected";

    public const string Conflict = "Conflict";

    /// <summary>The cloud expected an earlier sequence number for this patient first.</summary>
    public const string OutOfOrder = "OutOfOrder";

    /// <summary>Temporary cloud-side failure (e.g. database unavailable). Retried with backoff.</summary>
    public const string Transient = "Transient";

    /// <summary>Not processed because an earlier event of the same patient in this batch did
    /// not succeed.</summary>
    public const string NotAttempted = "NotAttempted";

    public static bool IsSuccess(string status) => status is Applied or Duplicate or Skipped;
}

/// <summary>How an admin resolves a blocked event from the Cloud Sync page.</summary>
public static class SyncResolutions
{
    /// <summary>Re-send the local version and let it overwrite the cloud copy (conflicts only).</summary>
    public const string LocalWins = "LocalWins";

    /// <summary>Copy the cloud version onto the local record (conflicts only). The local change
    /// is recorded as a new outbox event so both sides end up identical.</summary>
    public const string CloudWins = "CloudWins";

    /// <summary>Drop this event (NeedsAttention only). The patient's next event still carries
    /// the full record, so the two sides converge on the next change.</summary>
    public const string Skip = "Skip";

    public static readonly IReadOnlyList<string> All = [LocalWins, CloudWins, Skip];
}
