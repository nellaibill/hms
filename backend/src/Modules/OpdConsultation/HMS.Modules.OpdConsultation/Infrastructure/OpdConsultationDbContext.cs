using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.OpdConsultation.Infrastructure;

/// <summary>
/// Owns the "opd_consultation" PostgreSQL schema. Per docs/DatabaseArchitecture.md §1, only
/// this module's own code constructs/migrates this context — no other module references it.
/// </summary>
public class OpdConsultationDbContext : DbContext
{
    public const string SchemaName = "opd_consultation";

    public OpdConsultationDbContext(DbContextOptions<OpdConsultationDbContext> options) : base(options)
    {
    }

    // Internal (not public): the entity here is an internal domain type, so a public
    // DbSet<T> property would be a CS0053 accessibility violation. The context itself stays
    // public (HMS.Api's Program.cs/TenantMigrationService resolve it by type for the
    // startup/tenant migration call), but this DbSet is only ever queried from within this
    // module's repositories.
    internal DbSet<Domain.OpdConsultationNote> OpdConsultationNotes => Set<Domain.OpdConsultationNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OpdConsultationDbContext).Assembly);
    }
}
