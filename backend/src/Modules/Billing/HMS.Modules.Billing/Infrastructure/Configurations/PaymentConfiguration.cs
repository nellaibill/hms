using HMS.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Billing.Infrastructure.Configurations;

internal class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id).HasName("pk_payments");
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(p => p.InvoiceId).HasColumnName("invoice_id").IsRequired();
        builder.Property(p => p.InvoiceLineItemId).HasColumnName("invoice_line_item_id").IsRequired();
        builder.Property(p => p.Amount).HasColumnName("amount").HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(p => p.Method).HasColumnName("method").HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedBy).HasColumnName("created_by");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.UpdatedBy).HasColumnName("updated_by");
        builder.Property(p => p.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");
        builder.Property(p => p.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(p => !p.IsDeleted);

        // Restrict, not Cascade: unlike InvoiceLineItem, a Payment is an immutable ledger
        // entry that should never disappear as a side effect of its invoice/line item being
        // (soft-)deleted — mirrors AdmissionCharge's own reasoning against Admission.
        builder.HasOne<Invoice>()
            .WithMany()
            .HasForeignKey(p => p.InvoiceId)
            .HasConstraintName("fk_payments_invoices_invoice_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<InvoiceLineItem>()
            .WithMany()
            .HasForeignKey(p => p.InvoiceLineItemId)
            .HasConstraintName("fk_payments_invoice_line_items_invoice_line_item_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.InvoiceId).HasDatabaseName("ix_payments_invoice_id");
        builder.HasIndex(p => p.InvoiceLineItemId).HasDatabaseName("ix_payments_invoice_line_item_id");
    }
}
