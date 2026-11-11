using System.Runtime.CompilerServices;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using HMS.Shared.Kernel;
using HMS.Shared.Kernel.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Patients.Infrastructure.Sync;

/// <summary>
/// Offline-first sync capture (ADR-133). Added to <see cref="PatientsDbContext"/> only on an Edge
/// server (when an <see cref="IOutboxWriter"/> is registered), so it costs nothing anywhere else.
///
/// Every patient write path ends in one <c>PatientsDbContext.SaveChangesAsync</c> (registration,
/// edit, delete, allergy and emergency-contact add/remove, bulk import), so hooking that call
/// captures all of them without touching <c>PatientService</c>:
/// <list type="number">
/// <item><b>Saving:</b> find the patients whose aggregate changed. If the caller has no
/// transaction, start one; if it has one, join it and never commit it.</item>
/// <item>EF writes the patient rows inside that transaction.</item>
/// <item><b>Saved:</b> per patient, re-read the full aggregate, allocate the next sequence number
/// and insert the outbox row on the same connection and transaction, then commit (only a
/// transaction this interceptor started).</item>
/// <item><b>Failed / cancelled:</b> roll back, so neither the patient change nor its event
/// survives.</item>
/// </list>
/// A failure to write the event fails the save itself: a patient change can never commit without
/// its outbox event. Logs carry ids only, never patient values.
/// </summary>
internal sealed class PatientOutboxInterceptor : SaveChangesInterceptor
{
    private readonly IOutboxWriter _writer;
    private readonly ILogger<PatientOutboxInterceptor> _logger;

    // Keyed by context instance, so a captured change can never leak to another context.
    private readonly ConditionalWeakTable<DbContext, PendingCapture> _pending = new();

    public PatientOutboxInterceptor(IOutboxWriter writer, ILogger<PatientOutboxInterceptor> logger)
    {
        _writer = writer;
        _logger = logger;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not PatientsDbContext context)
        {
            return result;
        }

        var changes = PatientChangeCollector.Collect(context.ChangeTracker);
        if (changes.Count == 0)
        {
            return result;
        }

        var ownsTransaction = false;
        if (context.Database.CurrentTransaction is null)
        {
            await context.Database.BeginTransactionAsync(cancellationToken);
            ownsTransaction = true;
        }

        _pending.AddOrUpdate(context, new PendingCapture(changes, ownsTransaction));
        return result;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) =>
        SavingChangesAsync(eventData, result).AsTask().GetAwaiter().GetResult();

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not PatientsDbContext context || !_pending.TryGetValue(context, out var pending))
        {
            return result;
        }

        _pending.Remove(context);

        try
        {
            var transaction = context.Database.CurrentTransaction
                ?? throw new InvalidOperationException("The patient save finished without the transaction its outbox event must share.");
            var connection = context.Database.GetDbConnection();
            var dbTransaction = transaction.GetDbTransaction();

            // Patient-id order keeps the per-stream row locks in one global order, so two
            // saves touching the same patients can never deadlock on them.
            foreach (var (patientId, change) in pending.Changes.OrderBy(c => c.Key))
            {
                var patient = await LoadAggregateAsync(context, patientId, cancellationToken)
                    ?? throw new InvalidOperationException($"Patient {patientId} disappeared before its outbox event could be written.");

                var draft = new OutboxEventDraft(
                    PatientSyncAggregate.AggregateType,
                    patientId,
                    patientId,
                    change.Operation,
                    PatientSyncSnapshotMapper.Serialize(PatientSyncSnapshotMapper.ToSnapshot(patient)),
                    PatientSyncAggregate.CurrentPayloadSchemaVersion,
                    [.. change.Fields],
                    change.ActorId);

                var sequence = await _writer.WriteAsync(connection, dbTransaction, draft, cancellationToken);
                _logger.LogDebug(
                    "Sync outbox: captured {Operation} for patient {PatientId} as sequence {Sequence}.",
                    change.Operation,
                    patientId,
                    sequence);
            }

            if (pending.OwnsTransaction)
            {
                await context.Database.CommitTransactionAsync(cancellationToken);
            }
        }
        catch
        {
            if (pending.OwnsTransaction)
            {
                await RollBackQuietlyAsync(context);
            }

            throw;
        }

        return result;
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result) =>
        SavedChangesAsync(eventData, result).AsTask().GetAwaiter().GetResult();

    public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        => await AbandonAsync(eventData.Context);

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
        => AbandonAsync(eventData.Context).GetAwaiter().GetResult();

    public override async Task SaveChangesCanceledAsync(DbContextEventData eventData, CancellationToken cancellationToken = default)
        => await AbandonAsync(eventData.Context);

    public override void SaveChangesCanceled(DbContextEventData eventData)
        => AbandonAsync(eventData.Context).GetAwaiter().GetResult();

    private async Task AbandonAsync(DbContext? context)
    {
        if (context is null || !_pending.TryGetValue(context, out var pending))
        {
            return;
        }

        _pending.Remove(context);
        if (pending.OwnsTransaction)
        {
            await RollBackQuietlyAsync(context);
        }
    }

    private async Task RollBackQuietlyAsync(DbContext context)
    {
        try
        {
            if (context.Database.CurrentTransaction is not null)
            {
                await context.Database.RollbackTransactionAsync(CancellationToken.None);
            }
        }
        catch (Exception exception)
        {
            // The original failure is what the caller must see; a rollback failure just means
            // the connection is already gone, which rolls the transaction back anyway.
            _logger.LogWarning(exception, "Sync outbox: rollback after a failed patient save also failed.");
        }
    }

    private static Task<Patient?> LoadAggregateAsync(PatientsDbContext context, Guid patientId, CancellationToken cancellationToken) =>
        context.Patients
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(p => p.Address)
            .Include(p => p.Allergies)
            .Include(p => p.EmergencyContacts)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);

    private sealed record PendingCapture(IReadOnlyDictionary<Guid, CapturedChange> Changes, bool OwnsTransaction);
}

/// <summary>One patient's change within a save: the operation and the names (never the values)
/// of what changed.</summary>
internal sealed class CapturedChange
{
    public string Operation { get; set; } = OutboxOperationTypes.Update;
    public SortedSet<string> Fields { get; } = new(StringComparer.Ordinal);
    public Guid? ActorId { get; set; }
}

/// <summary>Reads the change tracker and groups Patient-aggregate changes by patient. Pure, so it
/// is unit-tested without a database.</summary>
internal static class PatientChangeCollector
{
    // Bookkeeping columns that change on every edit and say nothing about what was edited.
    private static readonly HashSet<string> IgnoredPatientProperties = new(StringComparer.Ordinal)
    {
        nameof(Entity.UpdatedAt),
        nameof(Entity.UpdatedBy),
        "xmin",
    };

    public static IReadOnlyDictionary<Guid, CapturedChange> Collect(ChangeTracker changeTracker)
    {
        changeTracker.DetectChanges();

        var changes = new Dictionary<Guid, CapturedChange>();
        foreach (var entry in changeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            switch (entry.Entity)
            {
                case Patient patient:
                    CollectPatient(changes, entry, patient);
                    break;
                case Address address:
                    Touch(changes, address.PatientId).Fields.Add(nameof(Patient.Address));
                    break;
                case Allergy allergy:
                    Touch(changes, allergy.PatientId).Fields.Add(nameof(Patient.Allergies));
                    break;
                case EmergencyContact contact:
                    Touch(changes, contact.PatientId).Fields.Add(nameof(Patient.EmergencyContacts));
                    break;
            }
        }

        return changes;
    }

    private static void CollectPatient(Dictionary<Guid, CapturedChange> changes, EntityEntry entry, Patient patient)
    {
        var change = Touch(changes, patient.Id);

        switch (entry.State)
        {
            case EntityState.Added:
                change.Operation = OutboxOperationTypes.Insert;
                change.ActorId = patient.CreatedBy;
                break;

            case EntityState.Deleted:
                change.Operation = OutboxOperationTypes.Delete;
                change.ActorId = patient.DeletedBy ?? patient.UpdatedBy;
                break;

            case EntityState.Modified:
                var isDeleted = entry.Property(nameof(Entity.IsDeleted));
                var softDeleted = isDeleted.IsModified && isDeleted.CurrentValue is true && isDeleted.OriginalValue is false;
                if (softDeleted && change.Operation != OutboxOperationTypes.Insert)
                {
                    change.Operation = OutboxOperationTypes.Delete;
                }

                change.ActorId = softDeleted ? patient.DeletedBy : patient.UpdatedBy;
                foreach (var property in entry.Properties)
                {
                    if (property.IsModified && !IgnoredPatientProperties.Contains(property.Metadata.Name))
                    {
                        change.Fields.Add(property.Metadata.Name);
                    }
                }

                break;
        }
    }

    private static CapturedChange Touch(Dictionary<Guid, CapturedChange> changes, Guid patientId)
    {
        if (!changes.TryGetValue(patientId, out var change))
        {
            change = new CapturedChange();
            changes[patientId] = change;
        }

        return change;
    }
}
