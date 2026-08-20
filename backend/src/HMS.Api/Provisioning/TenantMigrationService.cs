using HMS.Modules.Branding.Infrastructure;
using HMS.Modules.Calendar.Infrastructure;
using HMS.Modules.Documents.Infrastructure;
using HMS.Modules.HR.Infrastructure;
using HMS.Modules.Identity.Infrastructure;
using HMS.Modules.IPD.Infrastructure;
using HMS.Modules.Masters.Infrastructure;
using HMS.Modules.Patients.Infrastructure;
using HMS.Modules.Pharmacy.Infrastructure;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Products.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;

namespace HMS.Api.Provisioning;

/// <summary>
/// Implements HMS.Modules.Platform's <see cref="ITenantMigrationService"/> seam — see that
/// interface's own doc comment for why this lives in HMS.Api. Selective per Tenant
/// Feature/Module Management: each of the 9 real modules only migrates when its
/// FeatureCatalog key is present in the resolved (Mandatory-unioned) feature set, so a
/// tenant that never enabled e.g. "hr" never gets an hr schema (now 10 real modules, since
/// Pharmacy moved from FeatureCatalog.UiOnly to .SchemaBacked). The exact same module list
/// and order this always used to apply unconditionally — moved here so a brand-new tenant
/// (provisioning), an existing tenant's operator-triggered re-migrate, and a feature being
/// toggled on all go through one code path instead of duplicated migration lists drifting
/// apart.
/// </summary>
public sealed class TenantMigrationService : ITenantMigrationService
{
    public async Task MigrateAsync(string tenantConnectionString, IReadOnlyCollection<string> enabledFeatureKeys, CancellationToken cancellationToken)
    {
        // Defensive union: even if a caller's resolved set somehow omitted a mandatory
        // module (bug elsewhere, stale data), it still gets migrated here — this is the
        // single place that can never skip identity/masters/patients/documents/branding.
        var resolved = new HashSet<string>(enabledFeatureKeys);
        resolved.UnionWith(FeatureCatalog.Mandatory);

        if (resolved.Contains("identity"))
        {
            await using var db = new IdentityDbContext(BuildOptions<IdentityDbContext>(tenantConnectionString, IdentityDbContext.SchemaName));
            await db.Database.MigrateAsync(cancellationToken);
        }

        if (resolved.Contains("documents"))
        {
            await using var db = new DocumentsDbContext(BuildOptions<DocumentsDbContext>(tenantConnectionString, DocumentsDbContext.SchemaName));
            await db.Database.MigrateAsync(cancellationToken);
        }

        if (resolved.Contains("patients"))
        {
            await using var db = new PatientsDbContext(BuildOptions<PatientsDbContext>(tenantConnectionString, PatientsDbContext.SchemaName));
            await db.Database.MigrateAsync(cancellationToken);
        }

        if (resolved.Contains("branding"))
        {
            await using var db = new BrandingDbContext(BuildOptions<BrandingDbContext>(tenantConnectionString, BrandingDbContext.SchemaName));
            await db.Database.MigrateAsync(cancellationToken);
        }

        if (resolved.Contains("masters"))
        {
            await using var db = new MastersDbContext(BuildOptions<MastersDbContext>(tenantConnectionString, MastersDbContext.SchemaName));
            await db.Database.MigrateAsync(cancellationToken);
        }

        if (resolved.Contains("products"))
        {
            await using var db = new ProductsDbContext(BuildOptions<ProductsDbContext>(tenantConnectionString, ProductsDbContext.SchemaName));
            await db.Database.MigrateAsync(cancellationToken);
        }

        if (resolved.Contains("hr"))
        {
            await using var db = new HRDbContext(BuildOptions<HRDbContext>(tenantConnectionString, HRDbContext.SchemaName));
            await db.Database.MigrateAsync(cancellationToken);
        }

        if (resolved.Contains("calendar"))
        {
            await using var db = new CalendarDbContext(BuildOptions<CalendarDbContext>(tenantConnectionString, CalendarDbContext.SchemaName));
            await db.Database.MigrateAsync(cancellationToken);
        }

        if (resolved.Contains("ipd"))
        {
            await using var db = new IPDDbContext(BuildOptions<IPDDbContext>(tenantConnectionString, IPDDbContext.SchemaName));
            await db.Database.MigrateAsync(cancellationToken);
        }

        if (resolved.Contains("pharmacy"))
        {
            await using var db = new PharmacyDbContext(BuildOptions<PharmacyDbContext>(tenantConnectionString, PharmacyDbContext.SchemaName));
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
