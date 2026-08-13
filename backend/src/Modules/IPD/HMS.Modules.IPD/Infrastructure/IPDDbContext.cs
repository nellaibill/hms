using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.IPD.Infrastructure;

public class IPDDbContext : DbContext
{
    public const string SchemaName = "ipd";
    public const string AdmissionNumberSequenceName = "admission_no_seq";

    public IPDDbContext(DbContextOptions<IPDDbContext> options) : base(options)
    {
    }

    // Internal (not public): domain types are internal, so DbSet<T> properties stay internal too.
    internal DbSet<Ward> Wards => Set<Ward>();
    internal DbSet<Bed> Beds => Set<Bed>();
    internal DbSet<Admission> Admissions => Set<Admission>();
    internal DbSet<AdmissionCharge> AdmissionCharges => Set<AdmissionCharge>();
    internal DbSet<BedTransferHistory> BedTransferHistories => Set<BedTransferHistory>();
    internal DbSet<AdmissionBedStay> AdmissionBedStays => Set<AdmissionBedStay>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IPDDbContext).Assembly);

        modelBuilder.HasSequence<long>(AdmissionNumberSequenceName, SchemaName).StartsAt(1);
    }
}
