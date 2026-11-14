using System.Data.Common;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using HMS.Shared.Kernel.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace HMS.Modules.Patients.Infrastructure.Sync;

/// <summary>Implements <see cref="IPatientSyncService"/> (ADR-133). Logs ids and error codes
/// only, never patient values.</summary>
internal sealed class PatientSyncService : IPatientSyncService
{
    private const string UhidUniqueIndexName = "ux_patients_uhid";

    // One stamp over every row of the aggregate. xmin changes whenever a row is updated, so a
    // changed stamp means some part of the patient was written since the stamp was taken.
    private const string VersionStampSql = """
        SELECT encode(sha256(convert_to(string_agg(t.k, '|' ORDER BY t.k), 'UTF8')), 'hex')
        FROM (
            SELECT 'p:' || id::text || ':' || xmin::text AS k FROM patients.patients WHERE id = @id
            UNION ALL
            SELECT 'a:' || patient_id::text || ':' || xmin::text FROM patients.addresses WHERE patient_id = @id
            UNION ALL
            SELECT 'g:' || id::text || ':' || xmin::text FROM patients.allergies WHERE patient_id = @id
            UNION ALL
            SELECT 'e:' || id::text || ':' || xmin::text FROM patients.emergency_contacts WHERE patient_id = @id
        ) t
        """;

    private readonly PatientsDbContext _dbContext;
    private readonly IStateService _stateService;
    private readonly IDistrictService _districtService;
    private readonly ILogger<PatientSyncService> _logger;

    public PatientSyncService(
        PatientsDbContext dbContext,
        IStateService stateService,
        IDistrictService districtService,
        ILogger<PatientSyncService> logger)
    {
        _dbContext = dbContext;
        _stateService = stateService;
        _districtService = districtService;
        _logger = logger;
    }

    public async Task<PatientSyncApplyResult> ApplySnapshotAsync(
        DbConnection connection,
        DbTransaction transaction,
        string payloadJson,
        int payloadSchemaVersion,
        CancellationToken cancellationToken)
    {
        var prepared = await PrepareAsync(payloadJson, payloadSchemaVersion, cancellationToken);
        if (!prepared.Result.Succeeded)
        {
            return prepared.Result;
        }

        await using var context = CreateContext(connection);
        await context.Database.UseTransactionAsync(transaction, cancellationToken);
        return await ApplyStateAsync(context, prepared.State!, cancellationToken);
    }

    public async Task<string?> GetVersionStampAsync(DbConnection connection, DbTransaction? transaction, Guid patientId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = VersionStampSql;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "id";
        parameter.Value = patientId;
        command.Parameters.Add(parameter);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is string stamp ? stamp : null;
    }

    public async Task<string?> GetSnapshotJsonAsync(DbConnection connection, DbTransaction? transaction, Guid patientId, CancellationToken cancellationToken)
    {
        await using var context = CreateContext(connection);
        if (transaction is not null)
        {
            await context.Database.UseTransactionAsync(transaction, cancellationToken);
        }

        var patient = await LoadAggregate(context, asNoTracking: true).FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);
        return patient is null ? null : PatientSyncSnapshotMapper.Serialize(PatientSyncSnapshotMapper.ToSnapshot(patient));
    }

    public async Task<PatientSyncApplyResult> ApplySnapshotLocallyAsync(string payloadJson, CancellationToken cancellationToken)
    {
        var prepared = await PrepareAsync(payloadJson, PatientSyncAggregate.CurrentPayloadSchemaVersion, cancellationToken);
        if (!prepared.Result.Succeeded)
        {
            return prepared.Result;
        }

        // The DI-scoped context: on an Edge server it carries the outbox interceptor, so this
        // save is captured as a new event like any other patient edit.
        return await ApplyStateAsync(_dbContext, prepared.State!, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, PatientSyncDisplay>> GetDisplayInfoAsync(IReadOnlyCollection<Guid> patientIds, CancellationToken cancellationToken)
    {
        if (patientIds.Count == 0)
        {
            return new Dictionary<Guid, PatientSyncDisplay>();
        }

        var rows = await _dbContext.Patients
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(p => patientIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Uhid, p.FirstName, p.LastName, p.IsDeleted })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            r => r.Id,
            r => new PatientSyncDisplay(r.Id, r.Uhid, $"{r.FirstName} {r.LastName}".Trim(), r.IsDeleted));
    }

    public async Task<IReadOnlyList<Guid>> SearchPatientIdsAsync(string term, int limit, CancellationToken cancellationToken)
    {
        var trimmed = term.Trim();
        if (trimmed.Length == 0)
        {
            return [];
        }

        var pattern = "%" + trimmed.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
        return await _dbContext.Patients
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(p => p.Uhid == trimmed || EF.Functions.ILike(p.FirstName + " " + p.LastName, pattern, "\\"))
            .OrderBy(p => p.Id)
            .Select(p => p.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> BackfillOutboxAsync(IOutboxWriter writer, int batchSize, CancellationToken cancellationToken)
    {
        var written = 0;
        var offset = 0;

        while (true)
        {
            // Offset paging is stable here: the backfill never changes patient rows.
            var page = await LoadAggregate(_dbContext, asNoTracking: true)
                .OrderBy(p => p.Id)
                .Skip(offset)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
            if (page.Count == 0)
            {
                return written;
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var connection = _dbContext.Database.GetDbConnection();
            foreach (var patient in page)
            {
                var draft = new OutboxEventDraft(
                    PatientSyncAggregate.AggregateType,
                    patient.Id,
                    patient.Id,
                    OutboxOperationTypes.Snapshot,
                    PatientSyncSnapshotMapper.Serialize(PatientSyncSnapshotMapper.ToSnapshot(patient)),
                    PatientSyncAggregate.CurrentPayloadSchemaVersion,
                    [],
                    null);

                if (await writer.TryWriteInitialSnapshotAsync(connection, transaction.GetDbTransaction(), draft, cancellationToken))
                {
                    written++;
                }
            }

            await transaction.CommitAsync(cancellationToken);
            offset += page.Count;
            _logger.LogInformation("Sync backfill: {Written} snapshot event(s) written so far.", written);
        }
    }

    private async Task<(PatientSyncApplyResult Result, PatientReplicaState? State)> PrepareAsync(
        string payloadJson,
        int payloadSchemaVersion,
        CancellationToken cancellationToken)
    {
        if (!PatientSyncAggregate.SupportedPayloadSchemaVersions.Contains(payloadSchemaVersion))
        {
            return (PatientSyncApplyResult.Rejected(
                PatientSyncErrorCodes.UnsupportedPayloadVersion,
                $"Payload schema version {payloadSchemaVersion} is not supported by this server. Update this server first."), null);
        }

        var parsed = PatientSyncSnapshotMapper.TryParse(payloadJson, out var state);
        if (!parsed.Succeeded)
        {
            return (parsed, null);
        }

        // StateId/DistrictId point into Masters with no database foreign key, so they are
        // checked here, the same way PatientService checks them on registration. Either may be
        // blank where the hospital makes it optional (ADR-135); a blank one has nothing to check.
        var stateId = state!.Address.StateId;
        if (stateId is { } givenStateId)
        {
            var states = await _stateService.GetAllAsync(cancellationToken);
            if (!states.Any(s => s.Id == givenStateId))
            {
                return (PatientSyncApplyResult.Rejected(PatientSyncErrorCodes.InvalidState, "The patient's state does not exist on this server."), null);
            }
        }

        if (state.Address.DistrictId is { } givenDistrictId)
        {
            IReadOnlyList<DistrictResponse> districts = stateId is { } districtStateId
                ? await _districtService.GetByStateIdAsync(districtStateId, cancellationToken)
                : [];
            if (!districts.Any(d => d.Id == givenDistrictId))
            {
                return (PatientSyncApplyResult.Rejected(PatientSyncErrorCodes.InvalidDistrict, "The patient's district does not exist on this server for the given state."), null);
            }
        }

        return (PatientSyncApplyResult.Success(), state);
    }

    private async Task<PatientSyncApplyResult> ApplyStateAsync(PatientsDbContext context, PatientReplicaState state, CancellationToken cancellationToken)
    {
        var patient = await LoadAggregate(context, asNoTracking: false).FirstOrDefaultAsync(p => p.Id == state.Id, cancellationToken);

        try
        {
            if (patient is null)
            {
                context.Patients.Add(Patient.CreateReplica(state));
            }
            else
            {
                patient.ApplyReplicaState(state);
            }
        }
        catch (ArgumentException)
        {
            return PatientSyncApplyResult.Rejected(PatientSyncErrorCodes.InvalidPayload, "A required patient field is empty.");
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres && IsPermanent(postgres.SqlState))
        {
            _logger.LogWarning(
                "Sync apply for patient {PatientId} rejected by the database: {SqlState} on {Constraint}.",
                state.Id,
                postgres.SqlState,
                postgres.ConstraintName);

            // Leave the shared context clean for anything else the caller does with it.
            context.ChangeTracker.Clear();

            return postgres.SqlState switch
            {
                PostgresErrorCodes.UniqueViolation when postgres.ConstraintName == UhidUniqueIndexName =>
                    PatientSyncApplyResult.Rejected(PatientSyncErrorCodes.UhidConflict, "Another patient on this server already has this UHID."),
                PostgresErrorCodes.StringDataRightTruncation =>
                    PatientSyncApplyResult.Rejected(PatientSyncErrorCodes.ValueTooLong, "A patient field is longer than this server allows."),
                _ => PatientSyncApplyResult.Rejected(PatientSyncErrorCodes.ConstraintViolation, $"The database rejected the patient ({postgres.SqlState})."),
            };
        }

        return PatientSyncApplyResult.Success();
    }

    private static bool IsPermanent(string sqlState) => sqlState is
        PostgresErrorCodes.UniqueViolation or
        PostgresErrorCodes.StringDataRightTruncation or
        PostgresErrorCodes.NotNullViolation or
        PostgresErrorCodes.CheckViolation or
        PostgresErrorCodes.ForeignKeyViolation;

    private static IQueryable<Patient> LoadAggregate(PatientsDbContext context, bool asNoTracking)
    {
        IQueryable<Patient> query = context.Patients
            .IgnoreQueryFilters()
            .Include(p => p.Address)
            .Include(p => p.Allergies)
            .Include(p => p.EmergencyContacts)
            .AsSplitQuery();

        return asNoTracking ? query.AsNoTracking() : query;
    }

    // A context over someone else's open connection (the cloud receiver's), so the patient
    // writes join its transaction. No interceptor: the cloud never captures outbox events.
    private static PatientsDbContext CreateContext(DbConnection connection) =>
        new(new DbContextOptionsBuilder<PatientsDbContext>()
            .UseNpgsql(connection, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", PatientsDbContext.SchemaName))
            .Options);
}
