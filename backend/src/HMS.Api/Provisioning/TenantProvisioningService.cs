using System.Text.RegularExpressions;
using HMS.Modules.Branding.Infrastructure;
using HMS.Modules.Calendar.Infrastructure;
using HMS.Modules.Documents.Infrastructure;
using HMS.Modules.HR.Infrastructure;
using HMS.Modules.Identity;
using HMS.Modules.Identity.Infrastructure;
using HMS.Modules.IPD.Infrastructure;
using HMS.Modules.Masters.Infrastructure;
using HMS.Modules.Patients.Infrastructure;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Products.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
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
    private readonly ILogger<TenantProvisioningService> _logger;

    public TenantProvisioningService(IConfiguration configuration, ILogger<TenantProvisioningService> logger)
    {
        _platformAdminConnectionString = configuration.GetConnectionString("PlatformAdmin")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:PlatformAdmin' configuration value.");
        _logger = logger;
    }

    public async Task<Result<TenantProvisionResult>> ProvisionAsync(TenantProvisionRequest request, CancellationToken cancellationToken)
    {
        var databaseName = await GenerateUniqueDatabaseNameAsync(request.HospitalName, cancellationToken);

        await CreateDatabaseAsync(databaseName, cancellationToken);

        try
        {
            var tenantConnectionString = BuildTenantConnectionString(databaseName);

            await MigrateAllModulesAsync(tenantConnectionString, cancellationToken);

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
            _logger.LogError(ex, "Failed to roll back (drop) tenant database '{DatabaseName}' — manual cleanup required", databaseName);
        }
    }

    private string BuildTenantConnectionString(string databaseName)
    {
        var builder = new NpgsqlConnectionStringBuilder(_platformAdminConnectionString) { Database = databaseName };
        return builder.ConnectionString;
    }

    /// <summary>
    /// Applies every existing HMIS module's migrations to the tenant database, in the same
    /// order Program.cs's dev-migrate block uses. Only the 9 modules with real migrations
    /// today (see docs/DecisionLog.md's "Modules migrated" table) — Appointments/Billing are
    /// empty placeholder projects with no DbContext, so they're not included. Builds a fresh
    /// DbContextOptions per module rather than resolving the DI-registered instance, since
    /// those are permanently bound to ConnectionStrings:Default, not this new tenant
    /// database.
    /// </summary>
    private static async Task MigrateAllModulesAsync(string tenantConnectionString, CancellationToken cancellationToken)
    {
        await using (var db = new IdentityDbContext(BuildOptions<IdentityDbContext>(tenantConnectionString, IdentityDbContext.SchemaName)))
        {
            await db.Database.MigrateAsync(cancellationToken);
        }

        await using (var db = new DocumentsDbContext(BuildOptions<DocumentsDbContext>(tenantConnectionString, DocumentsDbContext.SchemaName)))
        {
            await db.Database.MigrateAsync(cancellationToken);
        }

        await using (var db = new PatientsDbContext(BuildOptions<PatientsDbContext>(tenantConnectionString, PatientsDbContext.SchemaName)))
        {
            await db.Database.MigrateAsync(cancellationToken);
        }

        await using (var db = new BrandingDbContext(BuildOptions<BrandingDbContext>(tenantConnectionString, BrandingDbContext.SchemaName)))
        {
            await db.Database.MigrateAsync(cancellationToken);
        }

        await using (var db = new MastersDbContext(BuildOptions<MastersDbContext>(tenantConnectionString, MastersDbContext.SchemaName)))
        {
            await db.Database.MigrateAsync(cancellationToken);
        }

        await using (var db = new ProductsDbContext(BuildOptions<ProductsDbContext>(tenantConnectionString, ProductsDbContext.SchemaName)))
        {
            await db.Database.MigrateAsync(cancellationToken);
        }

        await using (var db = new HRDbContext(BuildOptions<HRDbContext>(tenantConnectionString, HRDbContext.SchemaName)))
        {
            await db.Database.MigrateAsync(cancellationToken);
        }

        await using (var db = new CalendarDbContext(BuildOptions<CalendarDbContext>(tenantConnectionString, CalendarDbContext.SchemaName)))
        {
            await db.Database.MigrateAsync(cancellationToken);
        }

        await using (var db = new IPDDbContext(BuildOptions<IPDDbContext>(tenantConnectionString, IPDDbContext.SchemaName)))
        {
            await db.Database.MigrateAsync(cancellationToken);
        }
    }

    private static DbContextOptions<TContext> BuildOptions<TContext>(string connectionString, string schemaName)
        where TContext : DbContext
    {
        return new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", schemaName);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            })
            .Options;
    }
}
