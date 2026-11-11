using System.Data;
using System.Data.Common;
using HMS.Shared.Kernel.Sync;

namespace HMS.Modules.Sync.Infrastructure;

/// <summary>
/// Implements <see cref="IOutboxWriter"/> (ADR-133) with one SQL statement per event, on the
/// caller's connection and transaction:
/// <list type="bullet">
/// <item>the stream row is the per-patient sequence allocator: <c>INSERT ... ON CONFLICT DO UPDATE
/// SET last_sequence = last_sequence + 1 RETURNING</c>. Its row lock serialises concurrent
/// writers of the same patient only, and is held until the caller commits, so sequences are
/// gap-free (a rolled-back save rolls its number back too);</item>
/// <item>the event row is inserted with that number. The payload hash is computed by PostgreSQL
/// over the canonical jsonb text, so the cloud can recompute exactly the same hash.</item>
/// </list>
/// Stateless, so registered as a singleton.
/// </summary>
internal sealed class OutboxWriter : IOutboxWriter
{
    private const string WriteSql = """
        WITH seq AS (
            INSERT INTO sync.outbox_streams AS s (stream_id, aggregate_type, last_sequence, last_synced_sequence, status, retry_count, updated_at)
            VALUES (@stream_id, @aggregate_type, 1, 0, 'Pending', 0, now())
            ON CONFLICT (stream_id) DO UPDATE
                SET last_sequence = s.last_sequence + 1,
                    status = CASE WHEN s.status = 'Synced' THEN 'Pending' ELSE s.status END,
                    updated_at = now()
            RETURNING s.last_sequence
        ), body AS (
            SELECT CAST(@payload AS jsonb) AS payload
        )
        INSERT INTO sync.outbox_events (
            id, event_id, stream_id, aggregate_type, aggregate_id, operation_type, sequence_number,
            payload, payload_schema_version, payload_hash, changed_fields, status, retry_count, created_at, created_by)
        SELECT @id, @event_id, @stream_id, @aggregate_type, @aggregate_id, @operation_type, seq.last_sequence,
               body.payload, @payload_schema_version, encode(sha256(convert_to(body.payload::text, 'UTF8')), 'hex'),
               @changed_fields, 'Pending', 0, now(), @created_by
        FROM seq, body
        RETURNING sequence_number
        """;

    // Backfill: only when the stream has never existed (DO NOTHING yields no row, so nothing
    // is inserted and no number is returned).
    private const string InitialSnapshotSql = """
        WITH seq AS (
            INSERT INTO sync.outbox_streams (stream_id, aggregate_type, last_sequence, last_synced_sequence, status, retry_count, updated_at)
            VALUES (@stream_id, @aggregate_type, 1, 0, 'Pending', 0, now())
            ON CONFLICT (stream_id) DO NOTHING
            RETURNING last_sequence
        ), body AS (
            SELECT CAST(@payload AS jsonb) AS payload
        )
        INSERT INTO sync.outbox_events (
            id, event_id, stream_id, aggregate_type, aggregate_id, operation_type, sequence_number,
            payload, payload_schema_version, payload_hash, changed_fields, status, retry_count, created_at, created_by)
        SELECT @id, @event_id, @stream_id, @aggregate_type, @aggregate_id, @operation_type, seq.last_sequence,
               body.payload, @payload_schema_version, encode(sha256(convert_to(body.payload::text, 'UTF8')), 'hex'),
               @changed_fields, 'Pending', 0, now(), @created_by
        FROM seq, body
        RETURNING sequence_number
        """;

    public async Task<long> WriteAsync(DbConnection connection, DbTransaction transaction, OutboxEventDraft draft, CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(WriteSql, connection, transaction, draft, cancellationToken);
        return result ?? throw new InvalidOperationException("The outbox insert returned no sequence number.");
    }

    public async Task<bool> TryWriteInitialSnapshotAsync(DbConnection connection, DbTransaction transaction, OutboxEventDraft draft, CancellationToken cancellationToken)
        => await ExecuteAsync(InitialSnapshotSql, connection, transaction, draft, cancellationToken) is not null;

    private static async Task<long?> ExecuteAsync(string sql, DbConnection connection, DbTransaction transaction, OutboxEventDraft draft, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;

        Add(command, "id", Guid.CreateVersion7(), DbType.Guid);
        Add(command, "event_id", Guid.CreateVersion7(), DbType.Guid);
        Add(command, "stream_id", draft.StreamId, DbType.Guid);
        Add(command, "aggregate_type", draft.AggregateType, DbType.String);
        Add(command, "aggregate_id", draft.AggregateId, DbType.Guid);
        Add(command, "operation_type", draft.OperationType, DbType.String);
        Add(command, "payload", draft.PayloadJson, DbType.String);
        Add(command, "payload_schema_version", draft.PayloadSchemaVersion, DbType.Int32);
        Add(command, "created_by", draft.CreatedBy, DbType.Guid);

        // text[]: Npgsql infers the array type from the CLR string[].
        var changedFields = command.CreateParameter();
        changedFields.ParameterName = "changed_fields";
        changedFields.Value = draft.ChangedFields.ToArray();
        command.Parameters.Add(changedFields);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is long sequence ? sequence : null;
    }

    private static void Add(DbCommand command, string name, object? value, DbType type)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = type;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
