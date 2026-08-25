using HMS.Modules.Patients.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Patients.Infrastructure.Configurations;

/// <summary>Maps <see cref="Allergy"/> to patients.allergies — a 1:many child of Patient.</summary>
internal class AllergyConfiguration : IEntityTypeConfiguration<Allergy>
{
    public void Configure(EntityTypeBuilder<Allergy> builder)
    {
        builder.ToTable("allergies");

        builder.HasKey(a => a.Id).HasName("pk_allergies");
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(a => a.PatientId).HasColumnName("patient_id").IsRequired();

        builder.Property(a => a.AllergyType).HasColumnName("allergy_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.Specify).HasColumnName("specify").HasMaxLength(200);
        builder.Property(a => a.Severity).HasColumnName("severity").HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(a => a.PatientId).HasDatabaseName("ix_allergies_patient_id");
    }
}
