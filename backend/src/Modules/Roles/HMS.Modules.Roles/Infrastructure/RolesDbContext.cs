using HMS.Modules.Roles.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Roles.Infrastructure;

public class RolesDbContext : DbContext
{
    public const string SchemaName = "roles";

    public RolesDbContext(DbContextOptions<RolesDbContext> options)
        : base(options)
    {
    }

    internal DbSet<Role> Roles => Set<Role>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(RolesDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}