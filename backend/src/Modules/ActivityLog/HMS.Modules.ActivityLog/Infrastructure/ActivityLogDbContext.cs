using HMS.Modules.ActivityLog.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.ActivityLog.Infrastructure;

/// <summary>
/// Owns the "activity_log" PostgreSQL schema. Per docs/DatabaseArchitecture.md §1, only this
/// module's own code constructs/migrates this context — no other module references it.
/// </summary>
public class ActivityLogDbContext : DbContext
{
    public const string SchemaName = "activity_log";

    public ActivityLogDbContext(DbContextOptions<ActivityLogDbContext> options) : base(options)
    {
    }

    // Internal: the entity is an internal domain type (see MessagingDbContext).
    internal DbSet<ActivityLogEntry> Entries => Set<ActivityLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ActivityLogDbContext).Assembly);
    }
}
