using HMS.Modules.Patients.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Patients.Infrastructure.Configurations;

/// <summary>Maps <see cref="PatientVisitConsultation"/> to patients.patient_visit_consultations
/// — a 1:many child of PatientVisit.</summary>
internal class PatientVisitConsultationConfiguration : IEntityTypeConfiguration<PatientVisitConsultation>
{
    public void Configure(EntityTypeBuilder<PatientVisitConsultation> builder)
    {
        builder.ToTable("patient_visit_consultations");

        builder.HasKey(c => c.Id).HasName("pk_patient_visit_consultations");
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(c => c.VisitId).HasColumnName("visit_id").IsRequired();

        // App-level references into Masters' reference data (Department/Consultant/
        // ConsultationType) — no DB-level FK, see PatientVisitConfiguration's comment.
        builder.Property(c => c.DepartmentId).HasColumnName("department_id").IsRequired();
        builder.Property(c => c.ConsultantId).HasColumnName("consultant_id").IsRequired();
        builder.Property(c => c.ConsultationTypeId).HasColumnName("consultation_type_id");

        builder.HasIndex(c => c.VisitId).HasDatabaseName("ix_patient_visit_consultations_visit_id");
    }
}
