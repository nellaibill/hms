using HMS.Modules.Radiology.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Radiology.Infrastructure;

/// <summary>
/// Owns the "radiology" PostgreSQL schema. Per docs/DatabaseArchitecture.md §1, only this module's
/// own code constructs/migrates this context. Public because HMS.Api's TenantMigrationService
/// resolves it by type for the tenant migration call; the DbSet stays internal since its entity
/// is an internal domain type.
/// </summary>
public class RadiologyDbContext : DbContext
{
    public const string SchemaName = "radiology";

    public RadiologyDbContext(DbContextOptions<RadiologyDbContext> options) : base(options)
    {
    }

    internal DbSet<XrayAiAnalysis> XrayAiAnalyses => Set<XrayAiAnalysis>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RadiologyDbContext).Assembly);
    }
}
