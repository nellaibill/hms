using HMS.Modules.Branding.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Branding.Infrastructure;

/// <summary>
/// Owns the "branding" PostgreSQL schema. Per docs/DatabaseArchitecture.md §1, only this
/// module's own code constructs/migrates this context — no other module references it.
/// </summary>
public class BrandingDbContext : DbContext
{
    public const string SchemaName = "branding";

    public BrandingDbContext(DbContextOptions<BrandingDbContext> options) : base(options)
    {
    }

    // Internal (not public): BrandingSettings is an internal domain type, so a public
    // DbSet<T> property would be a CS0053 accessibility violation. Named "Settings" rather
    // than "BrandingSettings" to avoid colliding with the entity type's own name. The
    // context itself stays public (HMS.Api's Program.cs resolves it by type for the
    // startup migration call), but this DbSet is only ever queried from within this module.
    internal DbSet<BrandingSettings> Settings => Set<BrandingSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BrandingDbContext).Assembly);
    }
}
