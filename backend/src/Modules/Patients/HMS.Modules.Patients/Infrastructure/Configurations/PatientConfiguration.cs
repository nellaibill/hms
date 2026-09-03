using HMS.Modules.Patients.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Patients.Infrastructure.Configurations;

/// <summary>
/// Maps <see cref="Patient"/> to patients.patients. Internal (not public): Patient is an
/// internal domain type, so a public Configure(EntityTypeBuilder&lt;Patient&gt;) member would be
/// a CS0051 accessibility violation. EF Core's ApplyConfigurationsFromAssembly discovers and
/// invokes this via reflection regardless of the type's own visibility.
/// </summary>
internal class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients");

        builder.HasKey(p => p.Id).HasName("pk_patients");
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever(); // Generated in the domain (Guid.CreateVersion7()), not by the database.

        builder.Property(p => p.Uhid).HasColumnName("uhid").HasMaxLength(30).IsRequired();

        builder.Property(p => p.Title).HasColumnName("title").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
        builder.Property(p => p.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
        builder.Property(p => p.DateOfBirth).HasColumnName("date_of_birth").IsRequired();
        builder.Property(p => p.Gender).HasColumnName("gender").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.BloodGroup).HasColumnName("blood_group").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.MaritalStatus).HasColumnName("marital_status").HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(p => p.PrimaryPhone).HasColumnName("primary_phone").HasMaxLength(10).IsRequired();
        builder.Property(p => p.SecondaryPhone).HasColumnName("secondary_phone").HasMaxLength(10);
        builder.Property(p => p.Email).HasColumnName("email").HasMaxLength(256);
        builder.Property(p => p.Profession).HasColumnName("profession").HasMaxLength(100);

        builder.Property(p => p.IdProofType).HasColumnName("id_proof_type").HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.IdProofNumber).HasColumnName("id_proof_number").HasMaxLength(30);

        builder.Property(p => p.ModeOfArrivalSource).HasColumnName("mode_of_arrival_source").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(p => p.ModeOfArrivalChannel).HasColumnName("mode_of_arrival_channel").HasMaxLength(50);
        builder.Property(p => p.ModeOfArrivalSpecify).HasColumnName("mode_of_arrival_specify").HasMaxLength(200);

        builder.Property(p => p.RequiresDataVerification).HasColumnName("requires_data_verification").IsRequired().HasDefaultValue(false);

        // Standard audit columns.
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedBy).HasColumnName("created_by");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.UpdatedBy).HasColumnName("updated_by");
        builder.Property(p => p.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");
        builder.Property(p => p.DeletedBy).HasColumnName("deleted_by");

        // Optimistic concurrency via Postgres's own system column — no extra column needed.
        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => p.Uhid).IsUnique().HasDatabaseName("ux_patients_uhid").HasFilter("is_deleted = false");
        builder.HasIndex(p => new { p.FirstName, p.LastName }).HasDatabaseName("ix_patients_name");
        builder.HasIndex(p => p.PrimaryPhone).HasDatabaseName("ix_patients_primary_phone");
        builder.HasIndex(p => p.IdProofNumber).HasDatabaseName("ix_patients_id_proof_number");
        builder.HasIndex(p => p.RequiresDataVerification).HasDatabaseName("ix_patients_requires_data_verification").HasFilter("requires_data_verification = true");

        // Address is a true 1:1 — PatientId is Address's own primary key (see
        // AddressConfiguration), so this is a required one-to-one, not one-to-many.
        builder.HasOne(p => p.Address)
            .WithOne()
            .HasForeignKey<Address>(a => a.PatientId)
            .HasConstraintName("fk_addresses_patient_id")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasMany(p => p.Allergies)
            .WithOne()
            .HasForeignKey(a => a.PatientId)
            .HasConstraintName("fk_allergies_patient_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Allergies).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(p => p.EmergencyContacts)
            .WithOne()
            .HasForeignKey(c => c.PatientId)
            .HasConstraintName("fk_emergency_contacts_patient_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.EmergencyContacts).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
