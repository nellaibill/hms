using HMS.Modules.Platform.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Platform.Infrastructure;

/// <summary>
/// Owns the "platform" schema inside the hms_platform database (ConnectionStrings:Platform)
/// — see docs/DatabaseArchitecture.md's SaaS provisioning ADR. A real hospital tenant's own
/// schemas live in a separate physical database; local dev/the Windows installer happen to
/// point ConnectionStrings:Default at this same hms_platform database too (for Branding's
/// pre-login schema only, when Bootstrap:SeedLegacyTenant is false — see Program.cs), but
/// that's a connection-string choice, not something this context itself depends on. Only
/// this module's own code constructs/migrates this context — no other module references it.
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
    internal DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    internal DbSet<ProvisioningAlert> ProvisioningAlerts => Set<ProvisioningAlert>();
    internal DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();
    internal DbSet<PlatformMfaChallenge> PlatformMfaChallenges => Set<PlatformMfaChallenge>();
    internal DbSet<TenantFeature> TenantFeatures => Set<TenantFeature>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlatformDbContext).Assembly);
    }
}
