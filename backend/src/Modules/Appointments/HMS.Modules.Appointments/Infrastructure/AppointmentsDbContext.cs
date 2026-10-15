using HMS.Modules.Appointments.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Appointments.Infrastructure;

/// <summary>
/// Owns the "appointments" PostgreSQL schema. Per docs/DatabaseArchitecture.md §1, only this
/// module's own code constructs/migrates this context — no other module references it.
/// </summary>
public class AppointmentsDbContext : DbContext
{
    public const string SchemaName = "appointments";
    public const string AppointmentNumberSequenceName = "appointment_no_seq";

    public AppointmentsDbContext(DbContextOptions<AppointmentsDbContext> options) : base(options)
    {
    }

    // Internal: the entity types are internal, so public DbSet<T> properties would be a CS0053
    // accessibility violation. The context itself is public for HMS.Api's migration call.
    internal DbSet<Appointment> Appointments => Set<Appointment>();

    internal DbSet<ConsultantBlock> ConsultantBlocks => Set<ConsultantBlock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppointmentsDbContext).Assembly);
        modelBuilder.HasSequence<long>(AppointmentNumberSequenceName, SchemaName).StartsAt(1);
    }
}
