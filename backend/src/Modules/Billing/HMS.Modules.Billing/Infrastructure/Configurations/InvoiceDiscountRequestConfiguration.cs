using HMS.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Billing.Infrastructure.Configurations;

internal class InvoiceDiscountRequestConfiguration : IEntityTypeConfiguration<InvoiceDiscountRequest>
{
    public void Configure(EntityTypeBuilder<InvoiceDiscountRequest> builder)
    {
        builder.ToTable("invoice_discount_requests");

        builder.HasKey(r => r.Id).HasName("pk_invoice_discount_requests");
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(r => r.InvoiceId).HasColumnName("invoice_id").IsRequired();
        builder.Property(r => r.InvoiceNumber).HasColumnName("invoice_number").HasMaxLength(30).IsRequired();
        builder.Property(r => r.PatientId).HasColumnName("patient_id").IsRequired();
        builder.Property(r => r.PatientName).HasColumnName("patient_name").HasMaxLength(200).IsRequired();
        builder.Property(r => r.PatientUhid).HasColumnName("patient_uhid").HasMaxLength(30).IsRequired();
        builder.Property(r => r.GrossAmount).HasColumnName("gross_amount").HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(r => r.DiscountType).HasColumnName("discount_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.DiscountValue).HasColumnName("discount_value").HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(r => r.TotalDiscount).HasColumnName("total_discount").HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(r => r.Reason).HasColumnName("reason").HasMaxLength(500).IsRequired();
        builder.Property(r => r.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.RequestedByName).HasColumnName("requested_by_name").HasMaxLength(150);
        builder.Property(r => r.DecidedBy).HasColumnName("decided_by");
        builder.Property(r => r.DecidedByName).HasColumnName("decided_by_name").HasMaxLength(150);
        builder.Property(r => r.DecidedAt).HasColumnName("decided_at");
        builder.Property(r => r.DecisionNotes).HasColumnName("decision_notes").HasMaxLength(500);

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(r => r.CreatedBy).HasColumnName("created_by");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");
        builder.Property(r => r.UpdatedBy).HasColumnName("updated_by");
        builder.Property(r => r.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at");
        builder.Property(r => r.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(r => !r.IsDeleted);

        // Restrict, like the revert requests: a decided request is the audit record of who
        // approved a discount and must never disappear with its invoice.
        builder.HasOne<Invoice>()
            .WithMany()
            .HasForeignKey(r => r.InvoiceId)
            .HasConstraintName("fk_invoice_discount_requests_invoices_invoice_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.InvoiceId, "ix_invoice_discount_requests_invoice_id").HasDatabaseName("ix_invoice_discount_requests_invoice_id");
        builder.HasIndex(r => new { r.Status, r.CreatedAt }).HasDatabaseName("ix_invoice_discount_requests_status_created_at");

        // At most one Pending request per invoice — the backstop for two clerks raising one at
        // the same moment (InvoiceDiscountService checks first so it can return a proper error).
        builder.HasIndex(r => r.InvoiceId, "ux_invoice_discount_requests_invoice_id_pending")
            .IsUnique()
            .HasDatabaseName("ux_invoice_discount_requests_invoice_id_pending")
            .HasFilter("status = 'Pending' AND is_deleted = false");

        // Same backing-field ownership as Invoice.Items (InvoiceConfiguration).
        builder.HasMany(r => r.Lines)
            .WithOne()
            .HasForeignKey(l => l.RequestId)
            // Shortened: PostgreSQL truncates identifiers past 63 characters.
            .HasConstraintName("fk_invoice_discount_request_lines_requests_request_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(r => r.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal class InvoiceDiscountRequestLineConfiguration : IEntityTypeConfiguration<InvoiceDiscountRequestLine>
{
    public void Configure(EntityTypeBuilder<InvoiceDiscountRequestLine> builder)
    {
        builder.ToTable("invoice_discount_request_lines");

        builder.HasKey(l => l.Id).HasName("pk_invoice_discount_request_lines");
        builder.Property(l => l.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(l => l.RequestId).HasColumnName("request_id").IsRequired();
        builder.Property(l => l.LineItemId).HasColumnName("line_item_id").IsRequired();
        builder.Property(l => l.BillingType).HasColumnName("billing_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(l => l.DepartmentId).HasColumnName("department_id").HasMaxLength(100);
        builder.Property(l => l.ConsultantId).HasColumnName("consultant_id").HasMaxLength(100);
        builder.Property(l => l.ServiceId).HasColumnName("service_id").HasMaxLength(100);
        builder.Property(l => l.PackageId).HasColumnName("package_id");
        builder.Property(l => l.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(l => l.UnitPrice).HasColumnName("unit_price").HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(l => l.Discount).HasColumnName("discount").HasColumnType("numeric(12,2)").IsRequired();

        builder.Property(l => l.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(l => l.CreatedBy).HasColumnName("created_by");
        builder.Property(l => l.UpdatedAt).HasColumnName("updated_at");
        builder.Property(l => l.UpdatedBy).HasColumnName("updated_by");
        builder.Property(l => l.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(l => l.DeletedAt).HasColumnName("deleted_at");
        builder.Property(l => l.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(l => !l.IsDeleted);

        builder.HasIndex(l => l.RequestId).HasDatabaseName("ix_invoice_discount_request_lines_request_id");
    }
}
