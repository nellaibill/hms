using System.Data.Common;

namespace HMS.Shared.Kernel.Sync;

/// <summary>
/// The offline-first sync outbox seam (docs/modules/Sync/OfflineFirstSync.md, ADR-133).
/// A module that owns a synchronised aggregate (today: Patients) calls this from inside its own
/// save transaction, on its own <see cref="DbConnection"/> + <see cref="DbTransaction"/>, so the
/// business row and its outbox event commit or roll back together. Declared here (plain
/// System.Data.Common types, no EF/Npgsql) and implemented by HMS.Modules.Sync, so the owning
/// module never references Sync internals.
///
/// Registered in DI only on an Edge server (Sync:Role = Edge). On every other server
/// <c>sp.GetService&lt;IOutboxWriter&gt;()</c> is null and nothing is captured.
/// </summary>
public interface IOutboxWriter
{
    /// <summary>
    /// Allocates the next per-stream sequence number and inserts one outbox event, both on the
    /// caller's connection and transaction. Never commits. Throws on failure, which makes the
    /// caller's save fail and roll back, so a business change can never commit without its event.
    /// </summary>
    Task<long> WriteAsync(DbConnection connection, DbTransaction transaction, OutboxEventDraft draft, CancellationToken cancellationToken);

    /// <summary>
    /// Backfill only: writes <paramref name="draft"/> as sequence 1 when the stream has never had
    /// an event, and does nothing (returns false) when it already has one.
    /// </summary>
    Task<bool> TryWriteInitialSnapshotAsync(DbConnection connection, DbTransaction transaction, OutboxEventDraft draft, CancellationToken cancellationToken);
}

/// <summary>One aggregate change to record. <see cref="PayloadJson"/> is the full post-change
/// snapshot; <see cref="ChangedFields"/> holds property NAMES only (never values) for the
/// monitoring screen.</summary>
public sealed record OutboxEventDraft(
    string AggregateType,
    Guid AggregateId,
    Guid StreamId,
    string OperationType,
    string PayloadJson,
    int PayloadSchemaVersion,
    IReadOnlyList<string> ChangedFields,
    Guid? CreatedBy);

/// <summary>Operation types an outbox event can carry.</summary>
public static class OutboxOperationTypes
{
    public const string Insert = "Insert";
    public const string Update = "Update";
    public const string Delete = "Delete";

    /// <summary>A full snapshot written by the backfill command or by a "keep cloud version"
    /// conflict resolution, rather than by a user edit.</summary>
    public const string Snapshot = "Snapshot";
}
