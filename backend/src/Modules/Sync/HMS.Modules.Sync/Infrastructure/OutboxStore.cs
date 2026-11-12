using HMS.Modules.Sync.Application;
using HMS.Modules.Sync.Contracts;
using HMS.Modules.Sync.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace HMS.Modules.Sync.Infrastructure;

/// <summary>
/// The Edge server's outbox operations (ADR-133), each one short SQL statement or transaction on
/// the current hospital database. No transaction is ever held across the HTTP call to the cloud
/// (DatabaseArchitecture §10): a patient stream is claimed with a lease, the call is made, and the
/// results are written back in one short transaction, guarded by that lease.
/// </summary>
internal sealed class OutboxStore
{
    private readonly SyncDbContext _dbContext;

    public OutboxStore(SyncDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>Creates the single sync_state row (and with it the outbox epoch) if missing.</summary>
    public async Task<SyncState> GetStateAsync(CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            "INSERT INTO sync.sync_state (id, updated_at) VALUES (1, now()) ON CONFLICT (id) DO NOTHING",
            _ => { },
            cancellationToken);

        return await _dbContext.SyncStates.AsNoTracking().SingleAsync(s => s.Id == SyncState.SingletonId, cancellationToken);
    }

    /// <summary>
    /// Claims up to <paramref name="limit"/> patient streams that have unsynced events, are not
    /// blocked, are past their back-off and are not leased by a live worker.
    /// <c>FOR UPDATE SKIP LOCKED</c> means two workers (or two servers) never claim the same
    /// patient and never wait for each other; the lease then keeps the claim for the duration of
    /// the HTTP call without holding a transaction open. A crashed worker's lease simply expires.
    /// </summary>
    public async Task<IReadOnlyList<ClaimedStream>> ClaimStreamsAsync(string workerId, int limit, TimeSpan lease, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE sync.outbox_streams s
               SET lease_owner = @worker,
                   lease_expires_at = now() + make_interval(secs => @lease_seconds),
                   last_attempt_at = now()
             WHERE s.stream_id IN (
                   SELECT c.stream_id
                     FROM sync.outbox_streams c
                    WHERE c.last_synced_sequence < c.last_sequence
                      AND c.status NOT IN ('NeedsAttention', 'Conflict')
                      AND (c.next_attempt_at IS NULL OR c.next_attempt_at <= now())
                      AND (c.lease_expires_at IS NULL OR c.lease_expires_at < now())
                    ORDER BY c.next_attempt_at NULLS FIRST, c.stream_id
                    LIMIT @limit
                    FOR UPDATE SKIP LOCKED)
            RETURNING s.stream_id, s.last_synced_sequence
            """;

        var claimed = new List<ClaimedStream>();
        await QueryAsync(
            sql,
            parameters =>
            {
                parameters.AddWithValue("worker", workerId);
                parameters.AddWithValue("lease_seconds", lease.TotalSeconds);
                parameters.AddWithValue("limit", limit);
            },
            reader => claimed.Add(new ClaimedStream(reader.GetGuid(0), reader.GetInt64(1))),
            cancellationToken);

        return claimed;
    }

    /// <summary>The next unsynced events of each claimed stream (at most
    /// <paramref name="perStream"/> each), ordered by stream then sequence number.</summary>
    public async Task<IReadOnlyList<PendingEvent>> LoadEventsAsync(IReadOnlyCollection<Guid> streamIds, int perStream, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, event_id, stream_id, aggregate_type, aggregate_id, operation_type, sequence_number,
                   payload::text, payload_schema_version, payload_hash, created_at, retry_count, resolution
              FROM (
                    SELECT e.*, row_number() OVER (PARTITION BY e.stream_id ORDER BY e.sequence_number) AS rn
                      FROM sync.outbox_events e
                      JOIN sync.outbox_streams s ON s.stream_id = e.stream_id
                     WHERE e.stream_id = ANY(@stream_ids)
                       AND e.sequence_number > s.last_synced_sequence
                       AND e.status <> 'Synced'
                   ) e
             WHERE e.rn <= @per_stream
             ORDER BY e.stream_id, e.sequence_number
            """;

        var events = new List<PendingEvent>();
        await QueryAsync(
            sql,
            parameters =>
            {
                parameters.AddWithValue("stream_ids", streamIds.ToArray());
                parameters.AddWithValue("per_stream", perStream);
            },
            reader => events.Add(new PendingEvent(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetString(3),
                reader.GetGuid(4),
                reader.GetString(5),
                reader.GetInt64(6),
                reader.GetString(7),
                reader.GetInt32(8),
                reader.GetString(9),
                reader.GetDateTime(10),
                reader.GetInt32(11),
                reader.IsDBNull(12) ? null : reader.GetString(12))),
            cancellationToken);

        return events;
    }

    public Task MarkProcessingAsync(IReadOnlyCollection<Guid> outboxIds, CancellationToken cancellationToken) =>
        ExecuteAsync(
            "UPDATE sync.outbox_events SET status = 'Processing', last_attempt_at = now() WHERE id = ANY(@ids)",
            parameters => parameters.AddWithValue("ids", outboxIds.ToArray()),
            cancellationToken);

    /// <summary>A stream whose stored events do not continue from its last synced sequence. Should
    /// never happen (gap-free allocation, unique index); parked for an admin rather than guessed at.</summary>
    public Task BlockStreamAsync(Guid streamId, string workerId, string errorCode, CancellationToken cancellationToken) =>
        ExecuteAsync(
            """
            UPDATE sync.outbox_streams
               SET status = 'NeedsAttention', last_error_code = @code, lease_owner = NULL, lease_expires_at = NULL, updated_at = now()
             WHERE stream_id = @stream_id AND lease_owner = @worker
            """,
            parameters =>
            {
                parameters.AddWithValue("stream_id", streamId);
                parameters.AddWithValue("worker", workerId);
                parameters.AddWithValue("code", errorCode);
            },
            cancellationToken);

    /// <summary>Gives back leases (and puts in-flight events back to Pending) for streams this
    /// worker claimed but will not finish, e.g. on shutdown.</summary>
    public Task ReleaseAsync(IReadOnlyCollection<Guid> streamIds, string workerId, CancellationToken cancellationToken)
    {
        if (streamIds.Count == 0)
        {
            return Task.CompletedTask;
        }

        return ExecuteAsync(
            """
            WITH released AS (
                UPDATE sync.outbox_streams
                   SET lease_owner = NULL, lease_expires_at = NULL, updated_at = now()
                 WHERE stream_id = ANY(@ids) AND lease_owner = @worker
                RETURNING stream_id
            )
            UPDATE sync.outbox_events e
               SET status = 'Pending'
              FROM released r
             WHERE e.stream_id = r.stream_id AND e.status = 'Processing'
            """,
            parameters =>
            {
                parameters.AddWithValue("ids", streamIds.ToArray());
                parameters.AddWithValue("worker", workerId);
            },
            cancellationToken);
    }

    /// <summary>
    /// Writes one send's outcome in a single transaction: per stream, the stream row is updated
    /// only while this worker still holds its lease, and that stream's events (and attempt rows)
    /// are updated only if it did. Returns how many events became Synced.
    /// </summary>
    public async Task<int> ApplyPlansAsync(
        IReadOnlyList<StreamPlan> plans,
        string workerId,
        int? httpStatus,
        int durationMs,
        CancellationToken cancellationToken)
    {
        const string sql = """
            WITH s AS (
                UPDATE sync.outbox_streams
                   SET last_synced_sequence = GREATEST(last_synced_sequence, COALESCE(@last_synced, last_synced_sequence)),
                       status = CASE
                                    WHEN @head_status = 'Synced' AND GREATEST(last_synced_sequence, COALESCE(@last_synced, last_synced_sequence)) < last_sequence THEN 'Pending'
                                    ELSE @head_status
                                END,
                       retry_count = @head_retry_count,
                       next_attempt_at = CASE WHEN @delay_seconds IS NULL THEN NULL ELSE now() + make_interval(secs => @delay_seconds) END,
                       last_error_code = @error_code,
                       lease_owner = NULL,
                       lease_expires_at = NULL,
                       updated_at = now()
                 WHERE stream_id = @stream_id AND lease_owner = @worker
                RETURNING stream_id
            ), ev AS (
                SELECT * FROM unnest(@ids, @statuses, @retry_counts, @delays, @receipts, @processed, @codes, @messages, @snapshots)
                    AS t(id, status, retry_count, delay_seconds, receipt_id, processed_at, error_code, error_message, conflict_snapshot)
            ), updated AS (
                UPDATE sync.outbox_events e
                   SET status = ev.status,
                       retry_count = ev.retry_count,
                       next_retry_at = CASE WHEN ev.delay_seconds IS NULL THEN NULL ELSE now() + make_interval(secs => ev.delay_seconds) END,
                       synced_at = CASE WHEN ev.status = 'Synced' THEN now() ELSE e.synced_at END,
                       cloud_receipt_id = COALESCE(ev.receipt_id, e.cloud_receipt_id),
                       cloud_processed_at = COALESCE(ev.processed_at, e.cloud_processed_at),
                       error_code = ev.error_code,
                       error_message = ev.error_message,
                       conflict_snapshot = CASE WHEN ev.conflict_snapshot IS NULL THEN e.conflict_snapshot ELSE CAST(ev.conflict_snapshot AS jsonb) END
                  FROM ev
                 WHERE e.id = ev.id AND EXISTS (SELECT 1 FROM s)
                RETURNING e.id
            )
            SELECT count(*) FROM updated
            """;

        const string attemptsSql = """
            INSERT INTO sync.outbox_attempts (id, event_id, stream_id, attempt_no, worker_id, started_at, finished_at, outcome, http_status, error_code, error_message, duration_ms)
            SELECT t.id, t.event_id, t.stream_id,
                   COALESCE((SELECT max(a.attempt_no) FROM sync.outbox_attempts a WHERE a.event_id = t.event_id), 0) + 1,
                   @worker, now() - make_interval(secs => (@duration_ms / 1000.0)::double precision), now(), t.outcome, @http_status, t.error_code, t.error_message, @duration_ms
              FROM unnest(@ids, @event_ids, @stream_ids, @outcomes, @codes, @messages) AS t(id, event_id, stream_id, outcome, error_code, error_message)
            """;

        var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var syncedCount = 0;

        foreach (var plan in plans)
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            var transitions = plan.Transitions;
            command.Parameters.AddWithValue("stream_id", plan.StreamId);
            command.Parameters.AddWithValue("worker", workerId);
            command.Parameters.Add(new NpgsqlParameter("last_synced", NpgsqlDbType.Bigint) { Value = (object?)plan.LastSyncedSequence ?? DBNull.Value });
            command.Parameters.AddWithValue("head_status", plan.HeadStatus);
            command.Parameters.AddWithValue("head_retry_count", plan.HeadRetryCount);
            command.Parameters.Add(new NpgsqlParameter("delay_seconds", NpgsqlDbType.Double) { Value = (object?)plan.NextAttemptDelay?.TotalSeconds ?? DBNull.Value });
            command.Parameters.Add(new NpgsqlParameter("error_code", NpgsqlDbType.Text) { Value = (object?)plan.LastErrorCode ?? DBNull.Value });
            command.Parameters.AddWithValue("ids", transitions.Select(t => t.Event.Id).ToArray());
            command.Parameters.AddWithValue("statuses", transitions.Select(t => t.NewStatus).ToArray());
            command.Parameters.AddWithValue("retry_counts", transitions.Select(t => t.RetryCount).ToArray());
            command.Parameters.Add(Array("delays", NpgsqlDbType.Double, transitions.Select(t => t.RetryDelay?.TotalSeconds).ToArray()));
            command.Parameters.Add(Array("receipts", NpgsqlDbType.Uuid, transitions.Select(t => t.ReceiptId).ToArray()));
            command.Parameters.Add(Array("processed", NpgsqlDbType.TimestampTz, transitions.Select(t => AsUtc(t.ProcessedAt)).ToArray()));
            command.Parameters.Add(Array("codes", NpgsqlDbType.Text, transitions.Select(t => Truncate(t.ErrorCode, 100)).ToArray()));
            command.Parameters.Add(Array("messages", NpgsqlDbType.Text, transitions.Select(t => Truncate(t.ErrorMessage, 1000)).ToArray()));
            command.Parameters.Add(Array("snapshots", NpgsqlDbType.Text, transitions.Select(t => t.ConflictSnapshot).ToArray()));

            var updated = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            if (updated == 0)
            {
                // The lease was lost (another worker took the stream over). Its own run will
                // re-send these events; the cloud's EventId check makes that harmless.
                continue;
            }

            syncedCount += transitions.Count(t => t.Kind == EventTransitionKind.Synced);

            var attempts = transitions.Where(t => t.RecordAttempt).ToList();
            if (attempts.Count == 0)
            {
                continue;
            }

            await using var attemptCommand = new NpgsqlCommand(attemptsSql, connection, transaction);
            attemptCommand.Parameters.AddWithValue("worker", workerId);
            attemptCommand.Parameters.AddWithValue("duration_ms", durationMs);
            attemptCommand.Parameters.Add(new NpgsqlParameter("http_status", NpgsqlDbType.Integer) { Value = (object?)httpStatus ?? DBNull.Value });
            attemptCommand.Parameters.AddWithValue("ids", attempts.Select(_ => Guid.CreateVersion7()).ToArray());
            attemptCommand.Parameters.AddWithValue("event_ids", attempts.Select(t => t.Event.EventId).ToArray());
            attemptCommand.Parameters.AddWithValue("stream_ids", attempts.Select(t => t.Event.StreamId).ToArray());
            attemptCommand.Parameters.AddWithValue("outcomes", attempts.Select(t => t.Outcome).ToArray());
            attemptCommand.Parameters.Add(Array("codes", NpgsqlDbType.Text, attempts.Select(t => Truncate(t.ErrorCode, 100)).ToArray()));
            attemptCommand.Parameters.Add(Array("messages", NpgsqlDbType.Text, attempts.Select(t => Truncate(t.ErrorMessage, 1000)).ToArray()));
            await attemptCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return syncedCount;
    }

    /// <summary>Records the end of a tenant run.</summary>
    public Task RecordRunAsync(bool anySynced, bool? cloudReachable, bool authFailed, CancellationToken cancellationToken) =>
        ExecuteAsync(
            """
            UPDATE sync.sync_state
               SET last_run_at = now(),
                   last_successful_sync_at = CASE WHEN @any_synced THEN now() ELSE last_successful_sync_at END,
                   cloud_reachable = COALESCE(@reachable, cloud_reachable),
                   auth_failed = CASE WHEN @auth_failed THEN true ELSE auth_failed END,
                   updated_at = now()
             WHERE id = 1
            """,
            parameters =>
            {
                parameters.AddWithValue("any_synced", anySynced);
                parameters.Add(new NpgsqlParameter("reachable", NpgsqlDbType.Boolean) { Value = (object?)cloudReachable ?? DBNull.Value });
                parameters.AddWithValue("auth_failed", authFailed);
            },
            cancellationToken);

    /// <summary>Records a connectivity probe. A successful probe clears an earlier auth failure.</summary>
    public Task RecordProbeAsync(bool reachable, bool authFailed, string? error, int? latencyMs, CancellationToken cancellationToken) =>
        ExecuteAsync(
            """
            UPDATE sync.sync_state
               SET cloud_reachable = @reachable,
                   auth_failed = @auth_failed,
                   last_probe_at = now(),
                   last_probe_error = @error,
                   last_probe_latency_ms = @latency,
                   updated_at = now()
             WHERE id = 1
            """,
            parameters =>
            {
                parameters.AddWithValue("reachable", reachable);
                parameters.AddWithValue("auth_failed", authFailed);
                parameters.Add(new NpgsqlParameter("error", NpgsqlDbType.Text) { Value = (object?)Truncate(error, 500) ?? DBNull.Value });
                parameters.Add(new NpgsqlParameter("latency", NpgsqlDbType.Integer) { Value = (object?)latencyMs ?? DBNull.Value });
            },
            cancellationToken);

    /// <summary>Deletes synced events (and their attempts) older than the retention period.
    /// Unsynced events are never deleted, however old.</summary>
    public async Task<int> PurgeSyncedAsync(int retentionDays, CancellationToken cancellationToken)
    {
        var deleted = 0;
        await QueryAsync(
            """
            WITH old AS (
                DELETE FROM sync.outbox_events
                 WHERE status = 'Synced' AND synced_at < now() - make_interval(days => @days)
                RETURNING event_id
            ), attempts AS (
                DELETE FROM sync.outbox_attempts a USING old WHERE a.event_id = old.event_id
            )
            SELECT count(*) FROM old
            """,
            parameters => parameters.AddWithValue("days", retentionDays),
            reader => deleted = (int)reader.GetInt64(0),
            cancellationToken);

        return deleted;
    }

    public Task SetPausedAsync(bool paused, Guid? actorId, CancellationToken cancellationToken) =>
        ExecuteAsync(
            """
            UPDATE sync.sync_state
               SET is_paused = @paused,
                   paused_by = CASE WHEN @paused THEN @actor ELSE NULL END,
                   paused_at = CASE WHEN @paused THEN now() ELSE NULL END,
                   updated_at = now()
             WHERE id = 1
            """,
            parameters =>
            {
                parameters.AddWithValue("paused", paused);
                parameters.Add(new NpgsqlParameter("actor", NpgsqlDbType.Uuid) { Value = (object?)actorId ?? DBNull.Value });
            },
            cancellationToken);

    /// <summary>
    /// Manual retry: puts the blocked head event of each given stream (or of every Failed /
    /// NeedsAttention stream when <paramref name="streamIds"/> is null) back to Pending with a
    /// fresh retry count, and makes the stream claimable now. Conflicts are excluded: they are
    /// resolved, not retried. Returns the event ids that were reset.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> ResetForRetryAsync(IReadOnlyCollection<Guid>? streamIds, CancellationToken cancellationToken)
    {
        var reset = new List<Guid>();
        await QueryAsync(
            """
            WITH streams AS (
                UPDATE sync.outbox_streams
                   SET status = 'Pending', retry_count = 0, next_attempt_at = NULL, last_error_code = NULL, updated_at = now()
                 WHERE status IN ('Failed', 'NeedsAttention')
                   AND (@all OR stream_id = ANY(@ids))
                RETURNING stream_id, last_synced_sequence
            )
            UPDATE sync.outbox_events e
               SET status = 'Pending', retry_count = 0, next_retry_at = NULL, error_code = NULL, error_message = NULL
              FROM streams s
             WHERE e.stream_id = s.stream_id
               AND e.sequence_number = s.last_synced_sequence + 1
               AND e.status IN ('Failed', 'NeedsAttention')
            RETURNING e.event_id
            """,
            parameters =>
            {
                parameters.AddWithValue("all", streamIds is null);
                parameters.AddWithValue("ids", (streamIds ?? []).ToArray());
            },
            reader => reset.Add(reader.GetGuid(0)),
            cancellationToken);

        return reset;
    }

    /// <summary>
    /// Marks a blocked head event with an admin's resolution and puts it back in the queue. The
    /// event is then sent with that resolution: the cloud applies it without the conflict check
    /// (LocalWins) or records it as skipped and moves its sequence on (CloudWins / Skip). Only
    /// valid for the stream's current head in the expected status.
    /// </summary>
    public async Task<bool> MarkResolutionAsync(Guid eventId, string resolution, string expectedStatus, Guid? actorId, string? note, CancellationToken cancellationToken)
    {
        var updated = 0;
        await QueryAsync(
            """
            WITH target AS (
                SELECT e.id, e.stream_id
                  FROM sync.outbox_events e
                  JOIN sync.outbox_streams s ON s.stream_id = e.stream_id
                 WHERE e.event_id = @event_id
                   AND e.status = @expected
                   AND e.sequence_number = s.last_synced_sequence + 1
                 FOR UPDATE OF e, s
            ), events AS (
                UPDATE sync.outbox_events e
                   SET status = 'Pending', resolution = @resolution, resolution_note = @note, resolved_by = @actor, resolved_at = now(),
                       retry_count = 0, next_retry_at = NULL, error_code = NULL, error_message = NULL
                  FROM target t
                 WHERE e.id = t.id
                RETURNING e.stream_id
            ), streams AS (
                UPDATE sync.outbox_streams s
                   SET status = 'Pending', retry_count = 0, next_attempt_at = NULL, last_error_code = NULL, updated_at = now()
                  FROM events ev
                 WHERE s.stream_id = ev.stream_id
                RETURNING s.stream_id
            )
            SELECT count(*) FROM streams
            """,
            parameters =>
            {
                parameters.AddWithValue("event_id", eventId);
                parameters.AddWithValue("expected", expectedStatus);
                parameters.AddWithValue("resolution", resolution);
                parameters.Add(new NpgsqlParameter("note", NpgsqlDbType.Text) { Value = (object?)Truncate(note, 500) ?? DBNull.Value });
                parameters.Add(new NpgsqlParameter("actor", NpgsqlDbType.Uuid) { Value = (object?)actorId ?? DBNull.Value });
            },
            reader => updated = (int)reader.GetInt64(0),
            cancellationToken);

        return updated == 1;
    }

    /// <summary>The cloud's copy stored with a conflicting event (CloudWins needs it), or null.</summary>
    public async Task<string?> GetConflictSnapshotAsync(Guid eventId, CancellationToken cancellationToken)
    {
        string? snapshot = null;
        await QueryAsync(
            "SELECT conflict_snapshot::text FROM sync.outbox_events WHERE event_id = @event_id AND status = 'Conflict'",
            parameters => parameters.AddWithValue("event_id", eventId),
            reader => snapshot = reader.IsDBNull(0) ? null : reader.GetString(0),
            cancellationToken);

        return snapshot;
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        // EF owns this connection and closes it when the (scoped) context is disposed.
        await _dbContext.Database.OpenConnectionAsync(cancellationToken);
        return (NpgsqlConnection)_dbContext.Database.GetDbConnection();
    }

    private async Task ExecuteAsync(string sql, Action<NpgsqlParameterCollection> bind, CancellationToken cancellationToken)
    {
        var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        bind(command.Parameters);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task QueryAsync(string sql, Action<NpgsqlParameterCollection> bind, Action<NpgsqlDataReader> read, CancellationToken cancellationToken)
    {
        var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        bind(command.Parameters);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            read(reader);
        }
    }

    // Typed arrays (including nullable element types, written as SQL NULL elements), so unnest()
    // sees the right column types even when every element is null.
    private static NpgsqlParameter Array<T>(string name, NpgsqlDbType elementType, T[] values) =>
        new(name, NpgsqlDbType.Array | elementType) { Value = values };

    private static DateTime? AsUtc(DateTime? value) => value is { } v ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : null;

    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];
}

internal sealed record ClaimedStream(Guid StreamId, long LastSyncedSequence);
