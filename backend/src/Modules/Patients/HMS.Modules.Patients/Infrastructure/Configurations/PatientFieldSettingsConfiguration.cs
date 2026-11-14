using HMS.Modules.Patients.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Patients.Infrastructure.Configurations;

internal class PatientFieldSettingsConfiguration : IEntityTypeConfiguration<PatientFieldSettings>
{
    public void Configure(EntityTypeBuilder<PatientFieldSettings> builder)
    {
        builder.ToTable("patient_field_settings");

        builder.HasKey(s => s.Id).HasName("pk_patient_field_settings");
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.RequiredFieldsJson).HasColumnName("required_fields").HasColumnType("jsonb").IsRequired();

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.CreatedBy).HasColumnName("created_by");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");
        builder.Property(s => s.UpdatedBy).HasColumnName("updated_by");
        builder.Property(s => s.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(s => s.DeletedAt).HasColumnName("deleted_at");
        builder.Property(s => s.DeletedBy).HasColumnName("deleted_by");
    }
}
