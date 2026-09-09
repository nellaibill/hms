using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.IPD.Infrastructure.Configurations;

internal class AdmissionAdvanceConfiguration : IEntityTypeConfiguration<AdmissionAdvance>
{
    public void Configure(EntityTypeBuilder<AdmissionAdvance> builder)
    {
        builder.ToTable("admission_advances");

        builder.HasKey(a => a.Id).HasName("pk_admission_advances");
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.AdmissionId).HasColumnName("admission_id").IsRequired();
        builder.Property(a => a.Amount).HasColumnName("amount").HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(a => a.Method).HasColumnName("method").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.ReferenceNumber).HasColumnName("reference_number").HasMaxLength(100);
        builder.Property(a => a.Remarks).HasColumnName("remarks").HasMaxLength(500);

        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(a => a.CreatedBy).HasColumnName("created_by");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");
        builder.Property(a => a.UpdatedBy).HasColumnName("updated_by");
        builder.Property(a => a.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(a => a.DeletedAt).HasColumnName("deleted_at");
        builder.Property(a => a.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(a => !a.IsDeleted);

        builder.HasIndex(a => a.AdmissionId).HasDatabaseName("ix_admission_advances_admission_id");

        // No navigation collection on Admission itself — mirrors AdmissionCharge's own FK.
        // Restrict (not Cascade): Admission uses soft-delete, not hard delete, so there is no
        // hard-delete path this would guard against.
        builder.HasOne<Admission>()
            .WithMany()
            .HasForeignKey(a => a.AdmissionId)
            .HasConstraintName("fk_admission_advances_admissions_admission_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
