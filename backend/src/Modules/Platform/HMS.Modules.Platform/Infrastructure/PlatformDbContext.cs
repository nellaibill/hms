using HMS.Modules.Platform.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Platform.Infrastructure;

/// <summary>
/// Owns the "platform" schema inside the separate hms_platform database (not hms_qa —
/// see docs/DatabaseArchitecture.md's SaaS provisioning ADR). Only this module's own code
/// constructs/migrates this context — no other module references it.
/// </summary>
public class PlatformDbContext : DbContext
{
    public const string SchemaName = "platform";

    public PlatformDbContext(DbContextOptions<PlatformDbContext> options) : base(options)
    {
    }

    // Internal (not public): PlatformUser/Tenant are internal domain types, so public
    // DbSet<T> properties would be a CS0053 accessibility violation. The context itself
    // stays public (HMS.Api's Program.cs resolves it by type for the startup migration
    // call), but these DbSets are only ever queried from within this module.
    internal DbSet<PlatformUser> PlatformUsers => Set<PlatformUser>();
    internal DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlatformDbContext).Assembly);
    }
}
