using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.IPD.Infrastructure.Configurations;

internal class DoctorOrderConfiguration : IEntityTypeConfiguration<DoctorOrder>
{
    public void Configure(EntityTypeBuilder<DoctorOrder> builder)
    {
        builder.ToTable("doctor_orders");

        builder.HasKey(o => o.Id).HasName("pk_doctor_orders");
        builder.Property(o => o.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(o => o.AdmissionId).HasColumnName("admission_id").IsRequired();
        builder.Property(o => o.OrderType).HasColumnName("order_type").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(o => o.Description).HasColumnName("description").HasMaxLength(1000).IsRequired();
        builder.Property(o => o.Instructions).HasColumnName("instructions").HasMaxLength(2000);
        builder.Property(o => o.CatalogItemId).HasColumnName("catalog_item_id");
        builder.Property(o => o.OrderedAt).HasColumnName("ordered_at").IsRequired();
        builder.Property(o => o.OrderedByUserId).HasColumnName("ordered_by_user_id");
        builder.Property(o => o.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(o => o.CompletedAt).HasColumnName("completed_at");
        builder.Property(o => o.CancelledAt).HasColumnName("cancelled_at");
        builder.Property(o => o.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(1000);

        builder.Property(o => o.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(o => o.CreatedBy).HasColumnName("created_by");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at");
        builder.Property(o => o.UpdatedBy).HasColumnName("updated_by");
        builder.Property(o => o.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(o => o.DeletedAt).HasColumnName("deleted_at");
        builder.Property(o => o.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(o => !o.IsDeleted);

        builder.HasIndex(o => o.AdmissionId).HasDatabaseName("ix_doctor_orders_admission_id");

        // No navigation collection on Admission itself, same convention as every other IPD
        // child entity. Restrict (not Cascade): Admission uses soft-delete, not hard delete.
        builder.HasOne<Admission>()
            .WithMany()
            .HasForeignKey(o => o.AdmissionId)
            .HasConstraintName("fk_doctor_orders_admissions_admission_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
