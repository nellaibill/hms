using HMS.Modules.Laboratory.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Laboratory.Infrastructure;

public class LaboratoryDbContext : DbContext
{
    public const string SchemaName = "laboratory";
    public const string LabOrderNumberSequenceName = "lab_order_no_seq";

    public LaboratoryDbContext(DbContextOptions<LaboratoryDbContext> options) : base(options)
    {
    }

    // Internal (not public): domain types are internal, so DbSet<T> properties stay internal too.
    internal DbSet<LabOrder> LabOrders => Set<LabOrder>();
    internal DbSet<LabOrderItem> LabOrderItems => Set<LabOrderItem>();
    internal DbSet<LabResultParameter> LabResultParameters => Set<LabResultParameter>();
    internal DbSet<LabOrderItemEvent> LabOrderItemEvents => Set<LabOrderItemEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LaboratoryDbContext).Assembly);

        modelBuilder.HasSequence<long>(LabOrderNumberSequenceName, SchemaName).StartsAt(1);
    }
}
