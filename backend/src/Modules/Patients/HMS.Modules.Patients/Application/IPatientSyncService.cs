using System.Data.Common;
using HMS.Modules.Patients.Contracts;
using HMS.Shared.Kernel.Sync;

namespace HMS.Modules.Patients.Application;

/// <summary>
/// The Patients module's public seam for the offline-first sync (ADR-133), used only by
/// HMS.Modules.Sync. Public (not internal) for the same CS0051 reason as IPatientService: the
/// Sync module's public controllers depend on services that take it. Everything that writes
/// patient tables stays inside this module; Sync only passes a connection and transaction in.
/// </summary>
public interface IPatientSyncService
{
    /// <summary>Cloud side: applies a snapshot received from a local server, on the caller's
    /// connection and transaction (which the caller commits together with its inbox record).
    /// Returns a rejected result for permanent problems (bad payload, unknown state/district,
    /// UHID clash); throws for anything transient.</summary>
    Task<PatientSyncApplyResult> ApplySnapshotAsync(
        DbConnection connection,
        DbTransaction transaction,
        string payloadJson,
        int payloadSchemaVersion,
        CancellationToken cancellationToken);

    /// <summary>A hash of the (table, id, xmin) tuples of the whole aggregate. Any change to the
    /// patient, its address, allergies or contacts changes it. Null when the patient does not
    /// exist.</summary>
    Task<string?> GetVersionStampAsync(DbConnection connection, DbTransaction? transaction, Guid patientId, CancellationToken cancellationToken);

    /// <summary>The current snapshot JSON of a patient, or null when it does not exist.</summary>
    Task<string?> GetSnapshotJsonAsync(DbConnection connection, DbTransaction? transaction, Guid patientId, CancellationToken cancellationToken);

    /// <summary>Local side, "keep cloud version" conflict resolution: overwrites the local
    /// patient with the cloud's snapshot through the normal (outbox-captured) save, so the change
    /// is itself synced back as a new event.</summary>
    Task<PatientSyncApplyResult> ApplySnapshotLocallyAsync(string payloadJson, CancellationToken cancellationToken);

    /// <summary>UHID and name for the given patients (deleted ones included).</summary>
    Task<IReadOnlyDictionary<Guid, PatientSyncDisplay>> GetDisplayInfoAsync(IReadOnlyCollection<Guid> patientIds, CancellationToken cancellationToken);

    /// <summary>Ids of patients whose UHID equals, or whose name contains, <paramref name="term"/>.</summary>
    Task<IReadOnlyList<Guid>> SearchPatientIdsAsync(string term, int limit, CancellationToken cancellationToken);

    /// <summary>Backfill: writes a first snapshot event for every patient (deleted ones
    /// included) that has never had one. Safe to run repeatedly. Returns how many were written.</summary>
    Task<int> BackfillOutboxAsync(IOutboxWriter writer, int batchSize, CancellationToken cancellationToken);
}
