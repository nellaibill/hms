using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.IPD.Infrastructure.Configurations;

internal class BedTransferHistoryConfiguration : IEntityTypeConfiguration<BedTransferHistory>
{
    public void Configure(EntityTypeBuilder<BedTransferHistory> builder)
    {
        builder.ToTable("bed_transfer_history");

        builder.HasKey(h => h.Id).HasName("pk_bed_transfer_history");
        builder.Property(h => h.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(h => h.AdmissionId).HasColumnName("admission_id").IsRequired();
        builder.Property(h => h.OldWardId).HasColumnName("old_ward_id").IsRequired();
        builder.Property(h => h.OldBedId).HasColumnName("old_bed_id").IsRequired();
        builder.Property(h => h.NewWardId).HasColumnName("new_ward_id").IsRequired();
        builder.Property(h => h.NewBedId).HasColumnName("new_bed_id").IsRequired();
        builder.Property(h => h.TransferReason).HasColumnName("transfer_reason").HasMaxLength(500);

        builder.Property(h => h.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(h => h.CreatedBy).HasColumnName("created_by");
        builder.Property(h => h.UpdatedAt).HasColumnName("updated_at");
        builder.Property(h => h.UpdatedBy).HasColumnName("updated_by");
        builder.Property(h => h.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(h => h.DeletedAt).HasColumnName("deleted_at");
        builder.Property(h => h.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(h => !h.IsDeleted);

        builder.HasIndex(h => h.AdmissionId).HasDatabaseName("ix_bed_transfer_history_admission_id");

        // No navigation collection on Admission itself — mirrors AdmissionCharge's FK to
        // Admission. Restrict (not Cascade): Admission uses soft-delete, not hard delete.
        builder.HasOne<Admission>()
            .WithMany()
            .HasForeignKey(h => h.AdmissionId)
            .HasConstraintName("fk_bed_transfer_history_admissions_admission_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
