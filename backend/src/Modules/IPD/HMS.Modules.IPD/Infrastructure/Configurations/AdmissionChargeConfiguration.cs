using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.IPD.Infrastructure.Configurations;

internal class AdmissionChargeConfiguration : IEntityTypeConfiguration<AdmissionCharge>
{
    public void Configure(EntityTypeBuilder<AdmissionCharge> builder)
    {
        builder.ToTable("admission_charges");

        builder.HasKey(c => c.Id).HasName("pk_admission_charges");
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(c => c.AdmissionId).HasColumnName("admission_id").IsRequired();
        builder.Property(c => c.ChargeType).HasColumnName("charge_type").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(c => c.Amount).HasColumnName("amount").HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(c => c.Remarks).HasColumnName("remarks").HasMaxLength(500);

        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(c => c.CreatedBy).HasColumnName("created_by");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
        builder.Property(c => c.UpdatedBy).HasColumnName("updated_by");
        builder.Property(c => c.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(c => c.DeletedAt).HasColumnName("deleted_at");
        builder.Property(c => c.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.HasIndex(c => c.AdmissionId).HasDatabaseName("ix_admission_charges_admission_id");

        // No navigation collection on Admission itself — mirrors HR's ShiftAssignment -> Shift
        // FK. Restrict (not Cascade): Admission uses soft-delete, not hard delete, so there is
        // no hard-delete path this would guard against.
        builder.HasOne<Admission>()
            .WithMany()
            .HasForeignKey(c => c.AdmissionId)
            .HasConstraintName("fk_admission_charges_admissions_admission_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
