using HMS.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Identity.Infrastructure;

/// <summary>
/// Owns the "identity" PostgreSQL schema. Per docs/DatabaseArchitecture.md §1, only this
/// module's own code constructs/migrates this context — no other module references it.
/// </summary>
public class IdentityDbContext : DbContext
{
    public const string SchemaName = "identity";

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    // Internal (not public): User is an internal domain type, so a public property of
    // type DbSet<User> would be a CS0053 accessibility violation. The context itself stays
    // public (HMS.Api's Program.cs resolves it by type for the startup migration call),
    // but this DbSet is only ever queried from within this module (UserRepository).
    internal DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
