using HMS.Modules.Patients.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Patients.Infrastructure.Configurations;

/// <summary>
/// Maps <see cref="Patient"/> to patients.patients, following the naming/PK/audit-column/
/// soft-delete standards in docs/DatabaseArchitecture.md (same shape as Identity's
/// UserConfiguration — see docs/DeveloperHandbook.md §6/§12).
/// Internal (not public): Patient is an internal domain type, so a public
/// Configure(EntityTypeBuilder&lt;Patient&gt;) member would be a CS0051 accessibility
/// violation. EF Core's ApplyConfigurationsFromAssembly discovers and invokes this via
/// reflection regardless of the type's own visibility.
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
        builder.Property(p => p.BloodGroup).HasColumnName("blood_group").HasConversion<string>().HasMaxLength(20);

        builder.Property(p => p.AddressLine1).HasColumnName("address_line1").HasMaxLength(200).IsRequired();
        builder.Property(p => p.AddressLine2).HasColumnName("address_line2").HasMaxLength(200);
        builder.Property(p => p.AddressLine3).HasColumnName("address_line3").HasMaxLength(200);
        builder.Property(p => p.District).HasColumnName("district").HasMaxLength(100).IsRequired();
        builder.Property(p => p.State).HasColumnName("state").HasMaxLength(100).IsRequired();
        builder.Property(p => p.Pincode).HasColumnName("pincode").HasMaxLength(10).IsRequired();

        builder.Property(p => p.PrimaryPhone).HasColumnName("primary_phone").HasMaxLength(20).IsRequired();
        builder.Property(p => p.PrimaryPhoneRelation).HasColumnName("primary_phone_relation").HasMaxLength(50);
        builder.Property(p => p.AlternatePhone).HasColumnName("alternate_phone").HasMaxLength(20);
        builder.Property(p => p.AlternatePhoneRelation).HasColumnName("alternate_phone_relation").HasMaxLength(50);
        builder.Property(p => p.AlternatePhone2).HasColumnName("alternate_phone2").HasMaxLength(20);
        builder.Property(p => p.AlternatePhone2Relation).HasColumnName("alternate_phone2_relation").HasMaxLength(50);
        builder.Property(p => p.Email).HasColumnName("email").HasMaxLength(256);
        builder.Property(p => p.Profession).HasColumnName("profession").HasMaxLength(100);

        builder.Property(p => p.EmergencyContactRelationship).HasColumnName("emergency_contact_relationship").HasMaxLength(50).IsRequired();
        builder.Property(p => p.EmergencyContactName).HasColumnName("emergency_contact_name").HasMaxLength(150).IsRequired();
        builder.Property(p => p.EmergencyContactPhone).HasColumnName("emergency_contact_phone").HasMaxLength(20).IsRequired();

        builder.Property(p => p.HasKnownAllergy).HasColumnName("has_known_allergy").IsRequired();
        builder.Property(p => p.AllergyType).HasColumnName("allergy_type").HasMaxLength(200);
        builder.Property(p => p.AllergySeverity).HasColumnName("allergy_severity").HasConversion<string>().HasMaxLength(20);

        builder.Property(p => p.PhotoPath).HasColumnName("photo_path").HasMaxLength(500);
        builder.Property(p => p.IdProofType).HasColumnName("id_proof_type").HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.IdProofPath).HasColumnName("id_proof_path").HasMaxLength(500);

        // Standard audit columns (docs/DatabaseArchitecture.md §5).
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedBy).HasColumnName("created_by");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.UpdatedBy).HasColumnName("updated_by");
        builder.Property(p => p.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");
        builder.Property(p => p.DeletedBy).HasColumnName("deleted_by");

        // Optimistic concurrency via Postgres's own system column — no extra column needed
        // (docs/DatabaseArchitecture.md §5; the Npgsql provider's UseXminAsConcurrencyToken()
        // convenience method no longer exists in the referenced 10.0.0 package).
        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => p.Uhid).IsUnique().HasDatabaseName("ux_patients_uhid").HasFilter("is_deleted = false");
        builder.HasIndex(p => new { p.FirstName, p.LastName }).HasDatabaseName("ix_patients_name");
        builder.HasIndex(p => p.PrimaryPhone).HasDatabaseName("ix_patients_primary_phone");

        // Patient owns its registrations via the private _registrations backing field
        // (see Domain/Patient.cs's AddRegistration) — EF Core reads/writes through the
        // field rather than expecting a public setter on the read-only collection property.
        builder.HasMany(p => p.Registrations)
            .WithOne()
            .HasForeignKey(r => r.PatientId)
            .HasConstraintName("fk_patient_registrations_patient_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Registrations).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
