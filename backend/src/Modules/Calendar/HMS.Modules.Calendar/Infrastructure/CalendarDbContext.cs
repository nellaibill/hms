using HMS.Modules.Calendar.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Calendar.Infrastructure;

/// <summary>
/// Owns the "calendar" PostgreSQL schema. Per docs/DatabaseArchitecture.md §1, only
/// this module's own code constructs/migrates this context — no other module
/// references it.
/// </summary>
public class CalendarDbContext : DbContext
{
    public const string SchemaName = "calendar";

    public CalendarDbContext(DbContextOptions<CalendarDbContext> options) : base(options)
    {
    }

    // Internal (not public): Event is an internal domain type, so a public DbSet<T>
    // property would be a CS0053 accessibility violation. The context itself stays
    // public (HMS.Api's Program.cs resolves it by type for the startup migration
    // call), but this DbSet is only ever queried from within this module.
    internal DbSet<Event> Events => Set<Event>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CalendarDbContext).Assembly);
    }
}
