using System.Text.RegularExpressions;
using HMS.Modules.Identity;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Shared.Kernel;
using Npgsql;

namespace HMS.Api.Provisioning;

/// <summary>
/// Implements HMS.Modules.Platform's <see cref="ITenantProvisioner"/> seam — see that
/// interface's own doc comment for why this lives in HMS.Api rather than inside the
/// Platform module itself. Registered in HMS.Api/Configuration/ModuleRegistration.cs.
/// </summary>
public sealed class TenantProvisioningService : ITenantProvisioner
{
    // Postgres identifiers are capped at 63 bytes (NAMEDATALEN - 1); matches
    // Tenant.DatabaseName's column max length (TenantConfiguration.cs).
    private const int MaxDatabaseNameLength = 63;
    private static readonly Regex SafeIdentifier = new("^[a-z][a-z0-9_]*$", RegexOptions.Compiled);

    private readonly string _platformAdminConnectionString;
    private readonly ITenantMigrationService _migrationService;
    private readonly IProvisioningAlertStore _alertStore;
    private readonly ILogger<TenantProvisioningService> _logger;

    public TenantProvisioningService(
        IConfiguration configuration,
        ITenantMigrationService migrationService,
        IProvisioningAlertStore alertStore,
        ILogger<TenantProvisioningService> logger)
    {
        _platformAdminConnectionString = configuration.GetConnectionString("PlatformAdmin")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:PlatformAdmin' configuration value.");
        _migrationService = migrationService;
        _alertStore = alertStore;
        _logger = logger;
    }

    public async Task<Result<TenantProvisionResult>> ProvisionAsync(TenantProvisionRequest request, CancellationToken cancellationToken)
    {
        var databaseName = await GenerateUniqueDatabaseNameAsync(request.HospitalName, cancellationToken);

        await CreateDatabaseAsync(databaseName, cancellationToken);

        try
        {
            var tenantConnectionString = BuildTenantConnectionString(databaseName);

            await _migrationService.MigrateAsync(tenantConnectionString, request.EnabledFeatureKeys, cancellationToken);

            // Patients is a Mandatory module (always migrated regardless of EnabledFeatureKeys
            // — see TenantMigrationService), so both UHID sequences always exist by this point.
            // Sized once, here, to the Platform Admin's chosen boundary — the migration itself
            // always creates them at its own default (1-40000 / 40001+); this is a one-time
            // provisioning-time adjustment, not a schema change, so it doesn't belong in the
            // shared migration set every tenant gets identically.
            await SizeImportedPatientSequencesAsync(tenantConnectionString, request.ImportedPatientCapacity, cancellationToken);

            await IdentityModule.ProvisionTenantSuperAdminAsync(
                tenantConnectionString,
                request.SuperAdminUsername,
                request.SuperAdminFirstName,
                request.SuperAdminLastName,
                request.SuperAdminEmail,
                request.SuperAdminPhoneNumber,
                request.SuperAdminPassword,
                cancellationToken);

            return Result<TenantProvisionResult>.Success(new TenantProvisionResult(databaseName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tenant provisioning failed for database '{DatabaseName}' — rolling back", databaseName);

            await DropDatabaseAsync(databaseName, CancellationToken.None);

            return Result<TenantProvisionResult>.Failure(
                TenantProvisioningErrorCodes.Failed,
                "Failed to provision the hospital database. No changes were saved.");
        }
    }

    /// <summary>
    /// Sizes the Patients module's two UHID sequences to the requested boundary — safe to run
    /// unconditionally right after a fresh migration, before any patient exists in this
    /// database, so neither ALTER can ever fail against an already-consumed value.
    /// importedUhidCapacity is a validated .NET int (CreateHospitalRequestValidator), never a
    /// caller-supplied string, so interpolating it directly into this DDL carries no
    /// injection risk — Postgres doesn't support bind parameters for ALTER SEQUENCE literals
    /// at all, so this is the only way to express it.
    /// </summary>
    private static async Task SizeImportedPatientSequencesAsync(string tenantConnectionString, int importedPatientCapacity, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(tenantConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var alterImported = new NpgsqlCommand(
            $"ALTER SEQUENCE patients.imported_uhid_seq MAXVALUE {importedPatientCapacity}", connection))
        {
            await alterImported.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var alterUhid = new NpgsqlCommand(
            $"ALTER SEQUENCE patients.uhid_seq RESTART WITH {importedPatientCapacity + 1}", connection);
        await alterUhid.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Sanitizes the hospital name into a Postgres-safe database name (lowercase,
    /// non-alphanumeric runs collapsed to a single underscore, "hms_" prefixed), then
    /// appends a numeric suffix until it doesn't collide with an existing physical database.
    /// </summary>
    private async Task<string> GenerateUniqueDatabaseNameAsync(string hospitalName, CancellationToken cancellationToken)
    {
        var slug = Regex.Replace(hospitalName.Trim().ToLowerInvariant(), "[^a-z0-9]+", "_").Trim('_');
        if (string.IsNullOrEmpty(slug))
        {
            slug = "hospital";
        }

        var baseName = $"hms_{slug}";
        if (baseName.Length > MaxDatabaseNameLength)
        {
            baseName = baseName[..MaxDatabaseNameLength];
        }

        await using var connection = new NpgsqlConnection(_platformAdminConnectionString);
        await connection.OpenAsync(cancellationToken);

        var candidate = baseName;
        for (var suffix = 2; await DatabaseExistsAsync(connection, candidate, cancellationToken); suffix++)
        {
            var suffixText = $"_{suffix}";
            var truncatedBase = baseName.Length + suffixText.Length > MaxDatabaseNameLength
                ? baseName[..(MaxDatabaseNameLength - suffixText.Length)]
                : baseName;
            candidate = $"{truncatedBase}{suffixText}";
        }

        return candidate;
    }

    private static async Task<bool> DatabaseExistsAsync(NpgsqlConnection connection, string databaseName, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", connection);
        command.Parameters.AddWithValue("name", databaseName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    private async Task CreateDatabaseAsync(string databaseName, CancellationToken cancellationToken)
    {
        if (!SafeIdentifier.IsMatch(databaseName))
        {
            // Defense in depth: GenerateUniqueDatabaseNameAsync only ever produces names
            // matching this pattern, but CREATE DATABASE can't parameterize an identifier,
            // so this guards the raw-SQL interpolation below against any future change to
            // that generator introducing an unsafe character.
            throw new InvalidOperationException($"Generated database name '{databaseName}' is not a safe identifier.");
        }

        await using var connection = new NpgsqlConnection(_platformAdminConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", connection);
        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Created tenant database '{DatabaseName}'", databaseName);
    }

    private async Task DropDatabaseAsync(string databaseName, CancellationToken cancellationToken)
    {
        try
        {
            // Pooled Npgsql connections opened against the tenant database during migration
            // may still be alive; WITH (FORCE) (Postgres 13+) terminates them so the drop
            // doesn't fail with "database is being accessed by other users".
            NpgsqlConnection.ClearAllPools();

            await using var connection = new NpgsqlConnection(_platformAdminConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)", connection);
            await command.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation("Rolled back: dropped tenant database '{DatabaseName}'", databaseName);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to roll back (drop) tenant database '{DatabaseName}' — manual cleanup required", databaseName);

            // The log line above is easy to miss — this is the actual "alert": a durable
            // row that shows up in the Platform dashboard's stat tiles (see
            // ProvisioningAlert's own doc comment) so a human finds out even if nobody was
            // watching the logs when this happened. Deliberately best-effort: if writing the
            // alert itself fails (e.g. hms_platform is unreachable), that must not mask the
            // original rollback failure by throwing out of this catch block.
            try
            {
                await _alertStore.RaiseAsync(
                    databaseName,
                    $"Rollback failed after a provisioning error: {ex.Message}",
                    CancellationToken.None);
            }
            catch (Exception alertEx)
            {
                _logger.LogCritical(alertEx, "Failed to record the provisioning-rollback alert for database '{DatabaseName}' as well", databaseName);
            }
        }
    }

    private string BuildTenantConnectionString(string databaseName)
    {
        var builder = new NpgsqlConnectionStringBuilder(_platformAdminConnectionString) { Database = databaseName };
        return builder.ConnectionString;
    }
}
