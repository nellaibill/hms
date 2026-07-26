using HMS.Modules.Patients.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Patients.Infrastructure;

/// <summary>
/// Owns the "patients" PostgreSQL schema. Per docs/DatabaseArchitecture.md §1, only this
/// module's own code constructs/migrates this context — no other module references it.
/// </summary>
public class PatientsDbContext : DbContext
{
    public const string SchemaName = "patients";
    public const string UhidSequenceName = "uhid_seq";
    public const string RegistrationNumberSequenceName = "registration_no_seq";

    public PatientsDbContext(DbContextOptions<PatientsDbContext> options) : base(options)
    {
    }

    // Internal (not public): Patient/PatientRegistration are internal domain types, so
    // public DbSet<T> properties would be CS0053 accessibility violations. The context
    // itself stays public (HMS.Api's Program.cs resolves it by type for the startup
    // migration call), but these DbSets are only ever queried from within this module.
    internal DbSet<Patient> Patients => Set<Patient>();

    internal DbSet<PatientRegistration> PatientRegistrations => Set<PatientRegistration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PatientsDbContext).Assembly);

        // Back business identifiers (UHID, registration number) with real Postgres
        // sequences — coordination-free and gap-tolerant, unlike a MAX(...)+1 query.
        modelBuilder.HasSequence<long>(UhidSequenceName, SchemaName).StartsAt(1);
        modelBuilder.HasSequence<long>(RegistrationNumberSequenceName, SchemaName).StartsAt(1);
    }
}
