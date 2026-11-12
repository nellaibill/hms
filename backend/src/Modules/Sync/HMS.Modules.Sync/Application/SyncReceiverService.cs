using System.Data;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Sync.Contracts;
using HMS.Modules.Sync.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace HMS.Modules.Sync.Application;

/// <summary>The cloud's receiving side of the offline-first sync (ADR-133). Public for the same
/// CS0051 reason as other modules' service seams: SyncReceiverController depends on it.</summary>
public interface ISyncReceiverService
{
    /// <summary>True only on a Cloud server (Sync:Role = Cloud).</summary>
    bool AcceptsSyncTraffic { get; }

    /// <summary>Processes one batch for the hospital resolved from the caller's sync key.
    /// Returns one result per event, in request order.</summary>
    Task<SyncBatchResponse> ProcessAsync(SyncBatchRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Applies an Edge server's batch, one database transaction per event:
/// <list type="number">
/// <item><b>Idempotency:</b> an EventId already in sync.inbox_events returns the original result
/// (Duplicate) without touching any data.</item>
/// <item><b>Ordering:</b> the patient's replica_streams row is locked; a sequence number at or
/// below the last applied one is a Duplicate, one beyond the next expected is OutOfOrder.</item>
/// <item><b>Conflict check:</b> if the cloud copy's version stamp differs from the stamp recorded
/// right after the last sync apply, someone changed it outside sync: the conflict is recorded with
/// both versions and nothing is applied (no last-write-wins).</item>
/// <item><b>Apply:</b> the Patients module upserts the aggregate on the same connection and
/// transaction.</item>
/// <item><b>Acknowledge:</b> the inbox row and the new stream position are written and the
/// transaction commits, so the data change and its acknowledgement can never disagree.</item>
/// </list>
/// Within one patient, processing stops at the first event that does not succeed; the rest come
/// back NotAttempted. Other patients in the same batch are unaffected.
/// </summary>
internal sealed class SyncReceiverService : ISyncReceiverService
{
    public const int MaxEventsPerBatch = 1000;

    private readonly SyncDbContext _dbContext;
    private readonly IPatientSyncService _patientSync;
    private readonly SyncOptions _options;
    private readonly ILogger<SyncReceiverService> _logger;

    public SyncReceiverService(SyncDbContext dbContext, IPatientSyncService patientSync, IOptions<SyncOptions> options, ILogger<SyncReceiverService> logger)
    {
        _dbContext = dbContext;
        _patientSync = patientSync;
        _options = options.Value;
        _logger = logger;
    }

    public bool AcceptsSyncTraffic => _options.IsCloud;

    public async Task<SyncBatchResponse> ProcessAsync(SyncBatchRequest request, CancellationToken cancellationToken)
    {
        var results = new SyncEventResult[request.Events.Count];
        var blockedStreams = new HashSet<Guid>();

        for (var i = 0; i < request.Events.Count; i++)
        {
            var envelope = request.Events[i];
            if (blockedStreams.Contains(envelope.StreamId))
            {
                results[i] = Result(envelope, SyncResultStatuses.NotAttempted, message: "An earlier event of this patient did not succeed.");
                continue;
            }

            var result = await ProcessEventAsync(envelope, request, cancellationToken);
            results[i] = result;
            if (!SyncResultStatuses.IsSuccess(result.Status))
            {
                blockedStreams.Add(envelope.StreamId);
            }
        }

        return new SyncBatchResponse { Results = results, ServerTime = DateTime.UtcNow };
    }

    private async Task<SyncEventResult> ProcessEventAsync(SyncEventEnvelope envelope, SyncBatchRequest request, CancellationToken cancellationToken)
    {
        var validationError = Validate(envelope);
        if (validationError is not null)
        {
            return Result(envelope, SyncResultStatuses.Rejected, "SYNC.INVALID_EVENT", validationError);
        }

        await _dbContext.Database.OpenConnectionAsync(cancellationToken);
        var connection = (NpgsqlConnection)_dbContext.Database.GetDbConnection();
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            var result = await ProcessInTransactionAsync(connection, transaction, envelope, request, cancellationToken);
            if (result.Commit)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            return result.Result;
        }
        catch (Exception exception) when (IsTransient(exception) && !cancellationToken.IsCancellationRequested)
        {
            await RollBackQuietlyAsync(transaction);
            _logger.LogWarning(exception, "Sync event {EventId} for stream {StreamId} hit a transient database error.", envelope.EventId, envelope.StreamId);
            return Result(envelope, SyncResultStatuses.Transient, "SYNC.CLOUD_DB_UNAVAILABLE", "The cloud database is temporarily unavailable.");
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Unexpected (a bug, or data the code does not handle). Reported as Transient so the
            // sender backs off and, if it never clears, parks the event for an administrator.
            await RollBackQuietlyAsync(transaction);
            _logger.LogError(exception, "Sync event {EventId} for stream {StreamId} failed unexpectedly.", envelope.EventId, envelope.StreamId);
            return Result(envelope, SyncResultStatuses.Transient, "SYNC.CLOUD_ERROR", "The cloud could not process this event.");
        }
    }

    private async Task<(SyncEventResult Result, bool Commit)> ProcessInTransactionAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        SyncEventEnvelope envelope,
        SyncBatchRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Idempotency by EventId.
        var inbox = await FindInboxAsync(connection, transaction, envelope.EventId, cancellationToken);
        if (inbox is not null)
        {
            if (!string.Equals(inbox.Value.PayloadHash, envelope.PayloadHash, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Sync event {EventId} was re-sent with a different payload hash; the original result stands.", envelope.EventId);
            }

            return (Result(envelope, SyncResultStatuses.Duplicate, receiptId: inbox.Value.ReceiptId, processedAt: inbox.Value.ProcessedAt, message: "Already applied."), false);
        }

        // 2. Ordering, under a row lock on this patient's stream.
        var stream = await LockStreamAsync(connection, transaction, envelope, cancellationToken);
        var isNewEpoch = stream.SourceEpoch is { } epoch && epoch != request.SourceEpoch && request.SourceEpoch != Guid.Empty;
        var lastApplied = isNewEpoch ? 0 : stream.LastAppliedSequence;

        if (envelope.SequenceNumber <= lastApplied)
        {
            return (Result(envelope, SyncResultStatuses.Duplicate, message: "This sequence number was already applied."), false);
        }

        if (envelope.SequenceNumber > lastApplied + 1)
        {
            return (Result(envelope, SyncResultStatuses.OutOfOrder, "SYNC.OUT_OF_ORDER", $"Expected sequence {lastApplied + 1} first."), false);
        }

        // 3. Payload integrity: the hash is over PostgreSQL's canonical jsonb text on both sides.
        var payloadText = envelope.Payload.GetRawText();
        var hash = await ComputeHashAsync(connection, transaction, payloadText, cancellationToken);
        if (!string.Equals(hash, envelope.PayloadHash, StringComparison.OrdinalIgnoreCase))
        {
            return (Result(envelope, SyncResultStatuses.Rejected, "SYNC.PAYLOAD_HASH_MISMATCH", "The payload does not match its hash."), false);
        }

        var currentStamp = await _patientSync.GetVersionStampAsync(connection, transaction, envelope.AggregateId, cancellationToken);
        var receiptId = Guid.CreateVersion7();

        // 4a. Admin resolutions that apply nothing: move the sequence on and record the event.
        if (envelope.Resolution is SyncResolutions.Skip or SyncResolutions.CloudWins)
        {
            // CloudWins adopts the cloud's current copy as the new baseline; Skip keeps the old one.
            var baseline = envelope.Resolution == SyncResolutions.CloudWins ? currentStamp : stream.AppliedVersionStamp;
            await AcknowledgeAsync(connection, transaction, envelope, request, receiptId, SyncResultStatuses.Skipped, baseline, cancellationToken);
            if (envelope.Resolution == SyncResolutions.CloudWins)
            {
                await CloseConflictsAsync(connection, transaction, envelope, "ResolvedCloudWins", cancellationToken);
            }

            return (Result(envelope, SyncResultStatuses.Skipped, receiptId: receiptId, processedAt: DateTime.UtcNow, message: $"Recorded as {envelope.Resolution}."), true);
        }

        // 4b. Conflict check. A stream seen for the first time has no baseline: the local server
        // is the system of record (Decision 1), so its first event is adopted. A new epoch keeps
        // the old baseline: it still describes the cloud copy as sync last left it.
        if (stream.Exists
            && envelope.Resolution != SyncResolutions.LocalWins
            && !string.Equals(currentStamp, stream.AppliedVersionStamp, StringComparison.Ordinal))
        {
            var cloudSnapshot = await _patientSync.GetSnapshotJsonAsync(connection, transaction, envelope.AggregateId, cancellationToken);
            await RecordConflictAsync(connection, transaction, envelope, payloadText, cloudSnapshot, cancellationToken);
            _logger.LogWarning("Sync conflict on stream {StreamId} at sequence {Sequence}: the cloud copy changed outside sync.", envelope.StreamId, envelope.SequenceNumber);

            var conflict = Result(envelope, SyncResultStatuses.Conflict, "SYNC.CONFLICT", "The cloud copy of this patient was changed outside sync.");
            return (new SyncEventResult
            {
                EventId = conflict.EventId,
                Status = conflict.Status,
                ErrorCode = conflict.ErrorCode,
                Message = conflict.Message,
                CloudSnapshot = ParseJson(cloudSnapshot),
            }, true);
        }

        // 5. Apply through the Patients module, on this connection and transaction.
        var applied = await _patientSync.ApplySnapshotAsync(connection, transaction, payloadText, envelope.PayloadSchemaVersion, cancellationToken);
        if (!applied.Succeeded)
        {
            _logger.LogWarning("Sync event {EventId} for stream {StreamId} rejected: {ErrorCode}.", envelope.EventId, envelope.StreamId, applied.ErrorCode);
            return (Result(envelope, SyncResultStatuses.Rejected, applied.ErrorCode, applied.Message), false);
        }

        // 6. Acknowledge in the same transaction.
        var newStamp = await _patientSync.GetVersionStampAsync(connection, transaction, envelope.AggregateId, cancellationToken);
        await AcknowledgeAsync(connection, transaction, envelope, request, receiptId, SyncResultStatuses.Applied, newStamp, cancellationToken);
        if (envelope.Resolution == SyncResolutions.LocalWins)
        {
            await CloseConflictsAsync(connection, transaction, envelope, "ResolvedLocalWins", cancellationToken);
        }

        return (Result(envelope, SyncResultStatuses.Applied, receiptId: receiptId, processedAt: DateTime.UtcNow), true);
    }

    private static string? Validate(SyncEventEnvelope envelope)
    {
        if (envelope.EventId == Guid.Empty || envelope.StreamId == Guid.Empty || envelope.AggregateId == Guid.Empty)
        {
            return "EventId, StreamId and AggregateId are required.";
        }

        if (envelope.AggregateType != PatientSyncAggregate.AggregateType)
        {
            return $"Aggregate type '{envelope.AggregateType}' is not synchronised by this server.";
        }

        if (envelope.AggregateId != envelope.StreamId)
        {
            return "A patient event's stream id must be the patient id.";
        }

        if (envelope.SequenceNumber < 1)
        {
            return "SequenceNumber must be 1 or more.";
        }

        if (envelope.Payload.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            return "Payload must be a JSON object.";
        }

        if (string.IsNullOrWhiteSpace(envelope.PayloadHash))
        {
            return "PayloadHash is required.";
        }

        if (envelope.Resolution is not null && !SyncResolutions.All.Contains(envelope.Resolution))
        {
            return $"Resolution '{envelope.Resolution}' is not recognised.";
        }

        return null;
    }

    private static async Task<(Guid ReceiptId, DateTime ProcessedAt, string PayloadHash)?> FindInboxAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, Guid eventId, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT receipt_id, processed_at, payload_hash FROM sync.inbox_events WHERE event_id = @event_id",
            connection,
            transaction);
        command.Parameters.AddWithValue("event_id", eventId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? (reader.GetGuid(0), reader.GetDateTime(1), reader.GetString(2))
            : null;
    }

    private static async Task<StreamRow> LockStreamAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, SyncEventEnvelope envelope, CancellationToken cancellationToken)
    {
        // Two batches for the same patient (e.g. a retry racing an earlier, slow request)
        // serialise here; the second one then sees the first one's sequence number.
        await using var command = new NpgsqlCommand(
            """
            SELECT last_applied_sequence, applied_version_stamp, source_epoch
              FROM sync.replica_streams
             WHERE stream_id = @stream_id
               FOR UPDATE
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("stream_id", envelope.StreamId);
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                return new StreamRow(
                    true,
                    reader.GetInt64(0),
                    reader.IsDBNull(1) ? null : reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetGuid(2));
            }
        }

        // First event ever for this patient. A concurrent first event for the same patient waits
        // on this insert's unique key and then conflicts, which rolls it back as transient.
        await using var insert = new NpgsqlCommand(
            """
            INSERT INTO sync.replica_streams (stream_id, aggregate_type, last_applied_sequence, last_applied_event_id, applied_version_stamp, source_epoch, updated_at)
            VALUES (@stream_id, @aggregate_type, 0, @empty, NULL, NULL, now())
            """,
            connection,
            transaction);
        insert.Parameters.AddWithValue("stream_id", envelope.StreamId);
        insert.Parameters.AddWithValue("aggregate_type", envelope.AggregateType);
        insert.Parameters.AddWithValue("empty", Guid.Empty);
        await insert.ExecuteNonQueryAsync(cancellationToken);

        return new StreamRow(false, 0, null, null);
    }

    private static async Task<string> ComputeHashAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string payloadText, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT encode(sha256(convert_to(CAST(@payload AS jsonb)::text, 'UTF8')), 'hex')",
            connection,
            transaction);
        command.Parameters.Add(new NpgsqlParameter("payload", NpgsqlDbType.Text) { Value = payloadText });
        return (string)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task AcknowledgeAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        SyncEventEnvelope envelope,
        SyncBatchRequest request,
        Guid receiptId,
        string status,
        string? versionStamp,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO sync.inbox_events (event_id, stream_id, sequence_number, aggregate_type, operation_type, payload_hash, status, resolution, receipt_id, source_node_id, processed_at)
            VALUES (@event_id, @stream_id, @sequence, @aggregate_type, @operation_type, @payload_hash, @status, @resolution, @receipt_id, @source_node_id, now());

            UPDATE sync.replica_streams
               SET last_applied_sequence = @sequence,
                   last_applied_event_id = @event_id,
                   applied_version_stamp = @stamp,
                   source_epoch = @epoch,
                   updated_at = now()
             WHERE stream_id = @stream_id;
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("event_id", envelope.EventId);
        command.Parameters.AddWithValue("stream_id", envelope.StreamId);
        command.Parameters.AddWithValue("sequence", envelope.SequenceNumber);
        command.Parameters.AddWithValue("aggregate_type", envelope.AggregateType);
        command.Parameters.AddWithValue("operation_type", Truncate(envelope.OperationType, 20) ?? string.Empty);
        command.Parameters.AddWithValue("payload_hash", envelope.PayloadHash.ToLowerInvariant());
        command.Parameters.AddWithValue("status", status);
        command.Parameters.Add(new NpgsqlParameter("resolution", NpgsqlDbType.Text) { Value = (object?)envelope.Resolution ?? DBNull.Value });
        command.Parameters.AddWithValue("receipt_id", receiptId);
        command.Parameters.Add(new NpgsqlParameter("source_node_id", NpgsqlDbType.Text) { Value = (object?)Truncate(request.SourceNodeId, 100) ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter("stamp", NpgsqlDbType.Text) { Value = (object?)versionStamp ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter("epoch", NpgsqlDbType.Uuid) { Value = request.SourceEpoch == Guid.Empty ? DBNull.Value : request.SourceEpoch });
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task RecordConflictAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        SyncEventEnvelope envelope,
        string localSnapshot,
        string? cloudSnapshot,
        CancellationToken cancellationToken)
    {
        // One open conflict row per event: a re-send of the same conflicting event refreshes it.
        await using var command = new NpgsqlCommand(
            """
            WITH refreshed AS (
                UPDATE sync.conflicts
                   SET cloud_snapshot = CAST(@cloud AS jsonb), detected_at = now()
                 WHERE event_id = @event_id AND status = 'Open'
                RETURNING id
            )
            INSERT INTO sync.conflicts (id, stream_id, event_id, sequence_number, local_snapshot, cloud_snapshot, detected_at, status)
            SELECT @id, @stream_id, @event_id, @sequence, CAST(@local AS jsonb), CAST(@cloud AS jsonb), now(), 'Open'
             WHERE NOT EXISTS (SELECT 1 FROM refreshed)
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("stream_id", envelope.StreamId);
        command.Parameters.AddWithValue("event_id", envelope.EventId);
        command.Parameters.AddWithValue("sequence", envelope.SequenceNumber);
        command.Parameters.Add(new NpgsqlParameter("local", NpgsqlDbType.Text) { Value = localSnapshot });
        command.Parameters.Add(new NpgsqlParameter("cloud", NpgsqlDbType.Text) { Value = (object?)cloudSnapshot ?? DBNull.Value });
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task CloseConflictsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, SyncEventEnvelope envelope, string status, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            UPDATE sync.conflicts
               SET status = @status, resolved_at = now(), resolution_event_id = @event_id
             WHERE stream_id = @stream_id AND status = 'Open'
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("status", status);
        command.Parameters.AddWithValue("event_id", envelope.EventId);
        command.Parameters.AddWithValue("stream_id", envelope.StreamId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static bool IsTransient(Exception exception) => exception switch
    {
        NpgsqlException { IsTransient: true } => true,
        PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.UniqueViolation } => true,
        DbUpdateException { InnerException: { } inner } => IsTransient(inner),
        TimeoutException => true,
        _ => false,
    };

    private static async Task RollBackQuietlyAsync(NpgsqlTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch
        {
            // The connection is already gone, which rolls the transaction back anyway.
        }
    }

    private static SyncEventResult Result(
        SyncEventEnvelope envelope,
        string status,
        string? errorCode = null,
        string? message = null,
        Guid? receiptId = null,
        DateTime? processedAt = null) => new()
        {
            EventId = envelope.EventId,
            Status = status,
            ErrorCode = errorCode,
            Message = message,
            ReceiptId = receiptId,
            ProcessedAt = processedAt,
        };

    private static System.Text.Json.JsonElement? ParseJson(string? json)
    {
        if (json is null)
        {
            return null;
        }

        using var document = System.Text.Json.JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];

    private sealed record StreamRow(bool Exists, long LastAppliedSequence, string? AppliedVersionStamp, Guid? SourceEpoch);
}
