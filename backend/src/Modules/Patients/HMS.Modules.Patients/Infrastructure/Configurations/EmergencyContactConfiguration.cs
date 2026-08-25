using HMS.Modules.Patients.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Patients.Infrastructure.Configurations;

/// <summary>Maps <see cref="EmergencyContact"/> to patients.emergency_contacts — a 1:many
/// child of Patient.</summary>
internal class EmergencyContactConfiguration : IEntityTypeConfiguration<EmergencyContact>
{
    public void Configure(EntityTypeBuilder<EmergencyContact> builder)
    {
        builder.ToTable("emergency_contacts");

        builder.HasKey(c => c.Id).HasName("pk_emergency_contacts");
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(c => c.PatientId).HasColumnName("patient_id").IsRequired();

        builder.Property(c => c.Relationship).HasColumnName("relationship").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(c => c.Phone).HasColumnName("phone").HasMaxLength(10).IsRequired();

        builder.HasIndex(c => c.PatientId).HasDatabaseName("ix_emergency_contacts_patient_id");
    }
}
