using HMS.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Billing.Infrastructure.Configurations;

internal class InvoiceLineItemConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItem> builder)
    {
        builder.ToTable("invoice_line_items");

        builder.HasKey(li => li.Id).HasName("pk_invoice_line_items");
        builder.Property(li => li.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(li => li.InvoiceId).HasColumnName("invoice_id").IsRequired();
        builder.Property(li => li.BillingType).HasColumnName("billing_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(li => li.DepartmentId).HasColumnName("department_id").HasMaxLength(100);
        builder.Property(li => li.ConsultantId).HasColumnName("consultant_id").HasMaxLength(100);
        builder.Property(li => li.ServiceId).HasColumnName("service_id").HasMaxLength(100);
        builder.Property(li => li.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(li => li.UnitPrice).HasColumnName("unit_price").HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(li => li.Discount).HasColumnName("discount").HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(li => li.DiscountApproved).HasColumnName("discount_approved").IsRequired();
        builder.Property(li => li.DiscountApprovedBy).HasColumnName("discount_approved_by").HasMaxLength(150);
        builder.Property(li => li.PaymentStatus).HasColumnName("payment_status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(li => li.Total).HasColumnName("total").HasColumnType("numeric(12,2)").IsRequired();

        builder.Property(li => li.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(li => li.CreatedBy).HasColumnName("created_by");
        builder.Property(li => li.UpdatedAt).HasColumnName("updated_at");
        builder.Property(li => li.UpdatedBy).HasColumnName("updated_by");
        builder.Property(li => li.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(li => li.DeletedAt).HasColumnName("deleted_at");
        builder.Property(li => li.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(li => !li.IsDeleted);

        builder.HasIndex(li => li.InvoiceId).HasDatabaseName("ix_invoice_line_items_invoice_id");
    }
}
