using HMS.Modules.Patients.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Patients.Infrastructure.Configurations;

/// <summary>Maps <see cref="PatientRegistration"/> to patients.patient_registrations.</summary>
internal class PatientRegistrationConfiguration : IEntityTypeConfiguration<PatientRegistration>
{
    public void Configure(EntityTypeBuilder<PatientRegistration> builder)
    {
        builder.ToTable("patient_registrations");

        builder.HasKey(r => r.Id).HasName("pk_patient_registrations");
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(r => r.PatientId).HasColumnName("patient_id").IsRequired();
        builder.Property(r => r.RegistrationNumber).HasColumnName("registration_number").HasMaxLength(30).IsRequired();
        builder.Property(r => r.EncounterType).HasColumnName("encounter_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.ModeOfArrival).HasColumnName("mode_of_arrival").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.DepartmentId).HasColumnName("department_id").IsRequired();
        builder.Property(r => r.ConsultantId).HasColumnName("consultant_id").IsRequired();
        builder.Property(r => r.AdmissionType).HasColumnName("admission_type").HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.ReferralSource).HasColumnName("referral_source").HasMaxLength(200);
        builder.Property(r => r.Category).HasColumnName("category").HasMaxLength(100);

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(r => r.CreatedBy).HasColumnName("created_by");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");
        builder.Property(r => r.UpdatedBy).HasColumnName("updated_by");
        builder.Property(r => r.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at");
        builder.Property(r => r.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(r => !r.IsDeleted);

        builder.HasIndex(r => r.RegistrationNumber).IsUnique().HasDatabaseName("ux_patient_registrations_registration_number").HasFilter("is_deleted = false");
        builder.HasIndex(r => r.PatientId).HasDatabaseName("ix_patient_registrations_patient_id");
    }
}
