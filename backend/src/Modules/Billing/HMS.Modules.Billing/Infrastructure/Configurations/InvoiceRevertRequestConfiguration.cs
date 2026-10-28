using HMS.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Billing.Infrastructure.Configurations;

internal class InvoiceRevertRequestConfiguration : IEntityTypeConfiguration<InvoiceRevertRequest>
{
    public void Configure(EntityTypeBuilder<InvoiceRevertRequest> builder)
    {
        builder.ToTable("invoice_revert_requests");

        builder.HasKey(r => r.Id).HasName("pk_invoice_revert_requests");
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(r => r.InvoiceId).HasColumnName("invoice_id").IsRequired();
        builder.Property(r => r.InvoiceNumber).HasColumnName("invoice_number").HasMaxLength(30).IsRequired();
        builder.Property(r => r.PatientId).HasColumnName("patient_id").IsRequired();
        builder.Property(r => r.PatientName).HasColumnName("patient_name").HasMaxLength(200).IsRequired();
        builder.Property(r => r.PatientUhid).HasColumnName("patient_uhid").HasMaxLength(30).IsRequired();
        builder.Property(r => r.NetAmount).HasColumnName("net_amount").HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(r => r.PaidAmount).HasColumnName("paid_amount").HasColumnType("numeric(12,2)").IsRequired();
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

        // Restrict, like Payment: a decided request is the audit record of why a bill was
        // reverted and must never disappear with its invoice.
        builder.HasOne<Invoice>()
            .WithMany()
            .HasForeignKey(r => r.InvoiceId)
            .HasConstraintName("fk_invoice_revert_requests_invoices_invoice_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.InvoiceId, "ix_invoice_revert_requests_invoice_id").HasDatabaseName("ix_invoice_revert_requests_invoice_id");
        builder.HasIndex(r => new { r.Status, r.CreatedAt }).HasDatabaseName("ix_invoice_revert_requests_status_created_at");

        // At most one Pending request per invoice — the backstop for two clerks raising one at
        // the same moment (InvoiceRevertService checks first so it can return a proper error).
        builder.HasIndex(r => r.InvoiceId, "ux_invoice_revert_requests_invoice_id_pending")
            .IsUnique()
            .HasDatabaseName("ux_invoice_revert_requests_invoice_id_pending")
            .HasFilter("status = 'Pending' AND is_deleted = false");
    }
}
