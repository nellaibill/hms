using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.IPD.Infrastructure.Configurations;

internal class AdmissionConfiguration : IEntityTypeConfiguration<Admission>
{
    public void Configure(EntityTypeBuilder<Admission> builder)
    {
        builder.ToTable("admissions");

        builder.HasKey(a => a.Id).HasName("pk_admissions");
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.AdmissionNumber).HasColumnName("admission_number").HasMaxLength(30).IsRequired();
        builder.Property(a => a.PatientId).HasColumnName("patient_id").IsRequired();
        builder.Property(a => a.DepartmentId).HasColumnName("department_id").IsRequired();
        builder.Property(a => a.ConsultantId).HasColumnName("consultant_id").IsRequired();
        builder.Property(a => a.WardId).HasColumnName("ward_id").IsRequired();
        builder.Property(a => a.BedId).HasColumnName("bed_id").IsRequired();
        builder.Property(a => a.AdmissionDateTime).HasColumnName("admission_datetime").IsRequired();
        builder.Property(a => a.AdmissionType).HasColumnName("admission_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.ReasonForAdmission).HasColumnName("reason_for_admission").HasMaxLength(500).IsRequired();
        builder.Property(a => a.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(a => a.DischargeDateTime).HasColumnName("discharge_datetime");
        builder.Property(a => a.DischargeType).HasColumnName("discharge_type").HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.FinalDiagnosis).HasColumnName("final_diagnosis").HasMaxLength(1000);
        builder.Property(a => a.DischargeNotes).HasColumnName("discharge_notes").HasMaxLength(2000);
        builder.Property(a => a.FollowUpAdvice).HasColumnName("follow_up_advice").HasMaxLength(2000);

        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(a => a.CreatedBy).HasColumnName("created_by");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");
        builder.Property(a => a.UpdatedBy).HasColumnName("updated_by");
        builder.Property(a => a.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(a => a.DeletedAt).HasColumnName("deleted_at");
        builder.Property(a => a.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(a => !a.IsDeleted);

        builder.HasIndex(a => a.AdmissionNumber).IsUnique().HasDatabaseName("ux_admissions_admission_number").HasFilter("is_deleted = false");

        // Defense-in-depth against a race between two concurrent admissions: the app-level
        // "one active admission per patient" / "bed must be Available" checks in
        // AdmissionService are the primary guard, but a DB-level partial unique index closes
        // the TOCTOU gap between that check and the INSERT.
        builder.HasIndex(a => a.PatientId)
            .IsUnique()
            .HasDatabaseName("ux_admissions_active_patient")
            .HasFilter("status = 'Admitted' AND is_deleted = false");

        builder.HasIndex(a => a.BedId)
            .IsUnique()
            .HasDatabaseName("ux_admissions_active_bed")
            .HasFilter("status = 'Admitted' AND is_deleted = false");
    }
}
