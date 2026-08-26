using HMS.Modules.Patients.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Patients.Infrastructure.Configurations;

/// <summary>Maps <see cref="PatientVisit"/> to patients.patient_visits — an aggregate root
/// (own audit trail), not a child of Patient's own aggregate.</summary>
internal class PatientVisitConfiguration : IEntityTypeConfiguration<PatientVisit>
{
    public void Configure(EntityTypeBuilder<PatientVisit> builder)
    {
        builder.ToTable("patient_visits");

        builder.HasKey(v => v.Id).HasName("pk_patient_visits");
        builder.Property(v => v.Id)
            .HasColumnName("visit_id")
            .ValueGeneratedNever(); // Generated in the domain (Guid.CreateVersion7()), not by the database.

        builder.Property(v => v.PatientId).HasColumnName("patient_id").IsRequired();
        builder.Property(v => v.VisitType).HasColumnName("visit_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(v => v.AppointmentTypeId).HasColumnName("appointment_type_id");

        // Standard audit columns.
        builder.Property(v => v.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(v => v.CreatedBy).HasColumnName("created_by");
        builder.Property(v => v.UpdatedAt).HasColumnName("updated_at");
        builder.Property(v => v.UpdatedBy).HasColumnName("updated_by");
        builder.Property(v => v.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(v => v.DeletedAt).HasColumnName("deleted_at");
        builder.Property(v => v.DeletedBy).HasColumnName("deleted_by");

        // Optimistic concurrency via Postgres's own system column — no extra column needed.
        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(v => !v.IsDeleted);

        builder.HasIndex(v => v.PatientId).HasDatabaseName("ix_patient_visits_patient_id");

        // Same-module reference (unlike Department/Consultant/AppointmentType/ConsultationType
        // below, which point into Masters and stay app-level-only per docs/Architecture.md §4)
        // — a real DB-level FK is fine here. No navigation on either side: PatientVisit is its
        // own aggregate root, not a child of Patient's aggregate, so Patient never loads its
        // visits through EF navigation.
        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(v => v.PatientId)
            .HasConstraintName("fk_patient_visits_patient_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.Consultations)
            .WithOne()
            .HasForeignKey(c => c.VisitId)
            .HasConstraintName("fk_patient_visit_consultations_visit_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(v => v.Consultations).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
