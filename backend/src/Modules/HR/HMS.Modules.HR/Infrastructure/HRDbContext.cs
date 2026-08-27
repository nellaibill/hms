using HMS.Modules.HR.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.HR.Infrastructure;

/// <summary>
/// Owns the "hr" PostgreSQL schema — staff scheduling data (Shift for Phase 1; future
/// phases add ShiftAssignment, WeeklyRoster, StaffAvailability, ShiftSwapRequest to this
/// same context). Per docs/DatabaseArchitecture.md §1, only this module's own code
/// constructs/migrates this context — no other module references it.
/// </summary>
public class HRDbContext : DbContext
{
    public const string SchemaName = "hr";

    public HRDbContext(DbContextOptions<HRDbContext> options) : base(options)
    {
    }

    // Internal (not public): Shift is an internal domain type, so a public DbSet<T>
    // property would be a CS0053 accessibility violation. The context itself stays public
    // (HMS.Api's Program.cs resolves it by type for the startup migration call), but this
    // DbSet is only ever queried from within this module.
    internal DbSet<Shift> Shifts => Set<Shift>();
    internal DbSet<ShiftAssignment> ShiftAssignments => Set<ShiftAssignment>();
    internal DbSet<WeeklyRoster> WeeklyRosters => Set<WeeklyRoster>();
    internal DbSet<StaffAvailability> StaffAvailabilities => Set<StaffAvailability>();
    internal DbSet<ShiftSwapRequest> ShiftSwapRequests => Set<ShiftSwapRequest>();
    internal DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HRDbContext).Assembly);
    }
}
