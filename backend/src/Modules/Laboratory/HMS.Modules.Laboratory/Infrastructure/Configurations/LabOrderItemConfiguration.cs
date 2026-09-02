using HMS.Modules.Laboratory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Laboratory.Infrastructure.Configurations;

internal class LabOrderItemConfiguration : IEntityTypeConfiguration<LabOrderItem>
{
    public void Configure(EntityTypeBuilder<LabOrderItem> builder)
    {
        builder.ToTable("lab_order_items");

        builder.HasKey(i => i.Id).HasName("pk_lab_order_items");
        builder.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(i => i.LabOrderId).HasColumnName("lab_order_id").IsRequired();
        builder.Property(i => i.ServiceId).HasColumnName("service_id").IsRequired();
        builder.Property(i => i.PackageId).HasColumnName("package_id");
        builder.Property(i => i.InvoiceLineItemId).HasColumnName("invoice_line_item_id").IsRequired();
        builder.Property(i => i.TestName).HasColumnName("test_name").HasMaxLength(200).IsRequired();
        builder.Property(i => i.DepartmentId).HasColumnName("department_id");
        builder.Property(i => i.ConsultantId).HasColumnName("consultant_id");
        builder.Property(i => i.SampleType).HasColumnName("sample_type").HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.Property(i => i.CollectedAt).HasColumnName("collected_at");
        builder.Property(i => i.CollectedBy).HasColumnName("collected_by");
        builder.Property(i => i.CollectionLocation).HasColumnName("collection_location").HasMaxLength(200);
        builder.Property(i => i.SampleQuantity).HasColumnName("sample_quantity").HasMaxLength(50);
        builder.Property(i => i.CollectionRemarks).HasColumnName("collection_remarks").HasMaxLength(1000);

        builder.Property(i => i.RejectionReason).HasColumnName("rejection_reason").HasConversion<string>().HasMaxLength(30);
        builder.Property(i => i.RejectionRemarks).HasColumnName("rejection_remarks").HasMaxLength(1000);
        builder.Property(i => i.RejectedAt).HasColumnName("rejected_at");
        builder.Property(i => i.RejectedBy).HasColumnName("rejected_by");

        builder.Property(i => i.VerifiedAt).HasColumnName("verified_at");
        builder.Property(i => i.VerifiedBy).HasColumnName("verified_by");
        builder.Property(i => i.CorrectionReason).HasColumnName("correction_reason").HasMaxLength(1000);
        builder.Property(i => i.CorrectionRequestedAt).HasColumnName("correction_requested_at");
        builder.Property(i => i.CorrectionRequestedBy).HasColumnName("correction_requested_by");

        builder.Property(i => i.SubmittedForVerificationAt).HasColumnName("submitted_for_verification_at");
        builder.Property(i => i.SubmittedForVerificationBy).HasColumnName("submitted_for_verification_by");

        builder.Property(i => i.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(i => i.CreatedBy).HasColumnName("created_by");
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at");
        builder.Property(i => i.UpdatedBy).HasColumnName("updated_by");
        builder.Property(i => i.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(i => i.DeletedAt).HasColumnName("deleted_at");
        builder.Property(i => i.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(i => !i.IsDeleted);

        builder.HasIndex(i => i.LabOrderId).HasDatabaseName("ix_lab_order_items_lab_order_id");
        builder.HasIndex(i => i.PackageId).HasDatabaseName("ix_lab_order_items_package_id");
        builder.HasIndex(i => i.Status).HasDatabaseName("ix_lab_order_items_status");

        // LabOrderItem owns Parameters/Events via their private backing fields — same
        // convention as LabOrder.Items above.
        builder.HasMany(i => i.Parameters)
            .WithOne()
            .HasForeignKey(p => p.LabOrderItemId)
            .HasConstraintName("fk_lab_result_parameters_lab_order_items_lab_order_item_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(i => i.Parameters).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(i => i.Events)
            .WithOne()
            .HasForeignKey(e => e.LabOrderItemId)
            .HasConstraintName("fk_lab_order_item_events_lab_order_items_lab_order_item_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(i => i.Events).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
