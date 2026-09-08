using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.IPD.Infrastructure.Configurations;

internal class VitalsReadingConfiguration : IEntityTypeConfiguration<VitalsReading>
{
    public void Configure(EntityTypeBuilder<VitalsReading> builder)
    {
        builder.ToTable("vitals_readings");

        builder.HasKey(v => v.Id).HasName("pk_vitals_readings");
        builder.Property(v => v.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(v => v.AdmissionId).HasColumnName("admission_id").IsRequired();
        builder.Property(v => v.RecordedAt).HasColumnName("recorded_at").IsRequired();

        builder.Property(v => v.TemperatureF).HasColumnName("temperature_f").HasColumnType("numeric(5,2)");
        builder.Property(v => v.PulseRate).HasColumnName("pulse_rate");
        builder.Property(v => v.RespiratoryRate).HasColumnName("respiratory_rate");
        builder.Property(v => v.BloodPressureSystolic).HasColumnName("blood_pressure_systolic");
        builder.Property(v => v.BloodPressureDiastolic).HasColumnName("blood_pressure_diastolic");
        builder.Property(v => v.SpO2Percent).HasColumnName("spo2_percent");
        builder.Property(v => v.WeightKg).HasColumnName("weight_kg").HasColumnType("numeric(5,2)");
        builder.Property(v => v.HeightCm).HasColumnName("height_cm").HasColumnType("numeric(5,2)");
        builder.Property(v => v.PainScore).HasColumnName("pain_score");
        builder.Property(v => v.BloodGlucoseMgDl).HasColumnName("blood_glucose_mg_dl").HasColumnType("numeric(6,2)");
        builder.Property(v => v.RecordedByUserId).HasColumnName("recorded_by_user_id");
        builder.Property(v => v.Notes).HasColumnName("notes").HasMaxLength(1000);

        builder.Property(v => v.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(v => v.CreatedBy).HasColumnName("created_by");
        builder.Property(v => v.UpdatedAt).HasColumnName("updated_at");
        builder.Property(v => v.UpdatedBy).HasColumnName("updated_by");
        builder.Property(v => v.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(v => v.DeletedAt).HasColumnName("deleted_at");
        builder.Property(v => v.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(v => !v.IsDeleted);

        builder.HasIndex(v => v.AdmissionId).HasDatabaseName("ix_vitals_readings_admission_id");

        // No navigation collection on Admission itself, same convention as AdmissionCharge.
        // Restrict (not Cascade): Admission uses soft-delete, not hard delete.
        builder.HasOne<Admission>()
            .WithMany()
            .HasForeignKey(v => v.AdmissionId)
            .HasConstraintName("fk_vitals_readings_admissions_admission_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
