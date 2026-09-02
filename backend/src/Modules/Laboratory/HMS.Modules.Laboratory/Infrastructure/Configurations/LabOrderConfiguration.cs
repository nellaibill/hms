using HMS.Modules.Laboratory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Laboratory.Infrastructure.Configurations;

internal class LabOrderConfiguration : IEntityTypeConfiguration<LabOrder>
{
    public void Configure(EntityTypeBuilder<LabOrder> builder)
    {
        builder.ToTable("lab_orders");

        builder.HasKey(o => o.Id).HasName("pk_lab_orders");
        builder.Property(o => o.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(o => o.LabOrderNumber).HasColumnName("lab_order_number").HasMaxLength(30).IsRequired();
        builder.Property(o => o.InvoiceId).HasColumnName("invoice_id").IsRequired();
        builder.Property(o => o.PatientId).HasColumnName("patient_id").IsRequired();
        builder.Property(o => o.PatientName).HasColumnName("patient_name").HasMaxLength(200).IsRequired();
        builder.Property(o => o.PatientUhid).HasColumnName("patient_uhid").HasMaxLength(30).IsRequired();
        builder.Property(o => o.VisitId).HasColumnName("visit_id").IsRequired();
        builder.Property(o => o.Source).HasColumnName("source").HasMaxLength(30);
        builder.Property(o => o.Priority).HasColumnName("priority").HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(o => o.ReportGeneratedAt).HasColumnName("report_generated_at");
        builder.Property(o => o.ReportGeneratedBy).HasColumnName("report_generated_by");
        builder.Property(o => o.ReportReleasedAt).HasColumnName("report_released_at");
        builder.Property(o => o.ReportReleasedBy).HasColumnName("report_released_by");

        // OverallStatus is derived from Items + the report timestamps above (see
        // Domain/LabOrder.cs) — not a column.
        builder.Ignore(o => o.OverallStatus);

        builder.Property(o => o.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(o => o.CreatedBy).HasColumnName("created_by");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at");
        builder.Property(o => o.UpdatedBy).HasColumnName("updated_by");
        builder.Property(o => o.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(o => o.DeletedAt).HasColumnName("deleted_at");
        builder.Property(o => o.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(o => !o.IsDeleted);

        builder.HasIndex(o => o.LabOrderNumber).IsUnique().HasDatabaseName("ux_lab_orders_lab_order_number").HasFilter("is_deleted = false");

        // One LabOrder per Invoice, enforced at the database level — see
        // LabOrderService.CreateFromInvoiceAsync's own idempotency check, which this
        // backstops against a concurrent retry racing past the application-level check.
        builder.HasIndex(o => o.InvoiceId).IsUnique().HasDatabaseName("ux_lab_orders_invoice_id").HasFilter("is_deleted = false");

        builder.HasIndex(o => o.PatientId).HasDatabaseName("ix_lab_orders_patient_id");
        builder.HasIndex(o => o.CreatedAt).HasDatabaseName("ix_lab_orders_created_at");

        // LabOrder owns its items via the private _items backing field (see Domain/LabOrder.cs's
        // Create) — EF Core reads/writes through the field rather than expecting a public
        // setter on the read-only collection property. Mirrors Billing's Invoice.Items.
        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.LabOrderId)
            .HasConstraintName("fk_lab_order_items_lab_orders_lab_order_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
