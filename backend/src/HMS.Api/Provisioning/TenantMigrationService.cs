using HMS.Modules.Branding.Infrastructure;
using HMS.Modules.Calendar.Infrastructure;
using HMS.Modules.Documents.Infrastructure;
using HMS.Modules.HR.Infrastructure;
using HMS.Modules.Identity.Infrastructure;
using HMS.Modules.IPD.Infrastructure;
using HMS.Modules.Masters.Infrastructure;
using HMS.Modules.Patients.Infrastructure;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Products.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HMS.Api.Provisioning;

/// <summary>
/// Implements HMS.Modules.Platform's <see cref="ITenantMigrationService"/> seam — see that
/// interface's own doc comment for why this lives in HMS.Api. The exact same module list
/// and order <see cref="TenantProvisioningService"/> used to apply inline before this was
/// extracted — moved here so both a brand-new tenant (provisioning) and an existing tenant
/// (an explicit, operator-triggered migrate action — see
/// HMS.Modules.Platform.Endpoints.HospitalsController) go through one code path instead of
/// two copies of the same migration list drifting apart.
/// </summary>
public sealed class TenantMigrationService : ITenantMigrationService
{
    public async Task MigrateAsync(string tenantConnectionString, CancellationToken cancellationToken)
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
