using HMS.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Billing.Infrastructure.Configurations;

internal class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");

        builder.HasKey(i => i.Id).HasName("pk_invoices");
        builder.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(i => i.InvoiceNumber).HasColumnName("invoice_number").HasMaxLength(30).IsRequired();
        builder.Property(i => i.PatientId).HasColumnName("patient_id").IsRequired();
        builder.Property(i => i.VisitId).HasColumnName("visit_id").IsRequired();
        builder.Property(i => i.PatientName).HasColumnName("patient_name").HasMaxLength(200).IsRequired();
        builder.Property(i => i.PatientUhid).HasColumnName("patient_uhid").HasMaxLength(30).IsRequired();

        builder.Property(i => i.GrossAmount).HasColumnName("gross_amount").HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(i => i.TotalDiscount).HasColumnName("total_discount").HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(i => i.NetAmount).HasColumnName("net_amount").HasColumnType("numeric(12,2)").IsRequired();

        // PaymentStatus is derived from Items (see Domain/Invoice.cs) — not a column.
        builder.Ignore(i => i.PaymentStatus);

        builder.Property(i => i.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(i => i.CreatedBy).HasColumnName("created_by");
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at");
        builder.Property(i => i.UpdatedBy).HasColumnName("updated_by");
        builder.Property(i => i.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(i => i.DeletedAt).HasColumnName("deleted_at");
        builder.Property(i => i.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(i => !i.IsDeleted);

        builder.HasIndex(i => i.InvoiceNumber).IsUnique().HasDatabaseName("ux_invoices_invoice_number").HasFilter("is_deleted = false");
        builder.HasIndex(i => i.PatientId).HasDatabaseName("ix_invoices_patient_id");
        builder.HasIndex(i => i.CreatedAt).HasDatabaseName("ix_invoices_created_at");

        // Invoice owns its line items via the private _items backing field (see
        // Domain/Invoice.cs's Create) — EF Core reads/writes through the field rather than
        // expecting a public setter on the read-only collection property. Mirrors Patients'
        // Patient.Registrations (PatientConfiguration.cs).
        builder.HasMany(i => i.Items)
            .WithOne()
            .HasForeignKey(li => li.InvoiceId)
            .HasConstraintName("fk_invoice_line_items_invoices_invoice_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(i => i.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
