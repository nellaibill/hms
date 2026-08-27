using HMS.Modules.Patients.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Patients.Infrastructure;

/// <summary>
/// Owns the "patients" PostgreSQL schema. Only this module's own code constructs/migrates
/// this context — no other module references it.
/// </summary>
public class PatientsDbContext : DbContext
{
    public const string SchemaName = "patients";
    public const string UhidSequenceName = "uhid_seq";

    public PatientsDbContext(DbContextOptions<PatientsDbContext> options) : base(options)
    {
    }

    // Internal (not public): Patient is an internal domain type, so a public DbSet<T>
    // property would be a CS0053 accessibility violation. The context itself stays public
    // (HMS.Api's Program.cs / TenantMigrationService resolve it by type), but this DbSet is
    // only ever queried from within this module. Address/Allergy/EmergencyContact are
    // reachable only through Patient's navigation — no DbSet of their own.
    internal DbSet<Patient> Patients => Set<Patient>();

    // PatientVisit is its own aggregate root (not reached through Patient's navigation) —
    // Consultations, its 1:many child, has no DbSet of its own, reachable only via
    // PatientVisit.Consultations, same as Address/Allergy/EmergencyContact off Patient.
    internal DbSet<PatientVisit> PatientVisits => Set<PatientVisit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PatientsDbContext).Assembly);

        // Backs the UHID business identifier with a real Postgres sequence —
        // coordination-free and gap-tolerant, unlike a MAX(...)+1 query.
        modelBuilder.HasSequence<long>(UhidSequenceName, SchemaName).StartsAt(1);
    }
}
