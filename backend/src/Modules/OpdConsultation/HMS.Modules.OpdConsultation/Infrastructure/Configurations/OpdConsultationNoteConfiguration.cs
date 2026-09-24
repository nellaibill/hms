using HMS.Modules.OpdConsultation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.OpdConsultation.Infrastructure.Configurations;

/// <summary>
/// Maps <see cref="OpdConsultationNote"/> to opd_consultation.opd_consultation_notes. Internal
/// (not public): the entity is an internal domain type, so a public
/// Configure(EntityTypeBuilder&lt;OpdConsultationNote&gt;) member would be a CS0051
/// accessibility violation — EF Core's ApplyConfigurationsFromAssembly discovers and invokes
/// this via reflection regardless of the type's own visibility.
/// </summary>
internal class OpdConsultationNoteConfiguration : IEntityTypeConfiguration<OpdConsultationNote>
{
    public void Configure(EntityTypeBuilder<OpdConsultationNote> builder)
    {
        builder.ToTable("opd_consultation_notes");

        builder.HasKey(x => x.Id).HasName("pk_opd_consultation_notes");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        // No FK constraint to patients.* — cross-schema references are a deliberate, reviewed
        // exception (docs/DatabaseArchitecture.md §7), validated instead at the Application
        // layer via Patients' public IOpdQueryService/IPatientService.
        builder.Property(x => x.ConsultationId).HasColumnName("consultation_id").IsRequired();
        builder.Property(x => x.PatientId).HasColumnName("patient_id").IsRequired();
        builder.Property(x => x.VisitId).HasColumnName("visit_id").IsRequired();

        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(x => x.HeightCm).HasColumnName("height_cm").HasColumnType("numeric(5,2)");
        builder.Property(x => x.WeightKg).HasColumnName("weight_kg").HasColumnType("numeric(5,2)");
        builder.Property(x => x.PulseRate).HasColumnName("pulse_rate");
        builder.Property(x => x.BloodPressure).HasColumnName("blood_pressure").HasMaxLength(20);
        builder.Property(x => x.TemperatureF).HasColumnName("temperature_f").HasColumnType("numeric(5,2)");
        builder.Property(x => x.SpO2Percent).HasColumnName("spo2_percent");

        builder.Property(x => x.PresentingComplaints).HasColumnName("presenting_complaints").HasMaxLength(2000);
        builder.Property(x => x.ClinicalHistory).HasColumnName("clinical_history").HasMaxLength(4000);
        builder.Property(x => x.ExaminationFindings).HasColumnName("examination_findings").HasMaxLength(4000);

        builder.Property(x => x.PlanOfManagement).HasColumnName("plan_of_management").HasMaxLength(4000);

        builder.Property(x => x.ReviewDate).HasColumnName("review_date");
        builder.Property(x => x.FollowUpInstructions).HasColumnName("follow_up_instructions").HasMaxLength(2000);

        builder.Property(x => x.EmergencyReviewInstructions).HasColumnName("emergency_review_instructions").HasMaxLength(2000);

        builder.Property(x => x.ReferralDepartmentId).HasColumnName("referral_department_id");
        builder.Property(x => x.ReferralConsultantId).HasColumnName("referral_consultant_id");
        builder.Property(x => x.ReferralReason).HasColumnName("referral_reason").HasMaxLength(1000);

        // Standard audit columns (docs/DatabaseArchitecture.md §5).
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);

        // One note per consultation — the service layer already enforces this with a
        // check-then-create, this is the backstop, same convention as DischargeSummary's
        // identical AdmissionId uniqueness index.
        builder.HasIndex(x => x.ConsultationId).IsUnique().HasDatabaseName("ux_opd_consultation_notes_consultation_id").HasFilter("is_deleted = false");
        builder.HasIndex(x => x.PatientId).HasDatabaseName("ix_opd_consultation_notes_patient_id");

        // Real, same-schema FKs — unlike ConsultationId/PatientId/VisitId above, these are
        // intra-module relationships. Cascade: a note is soft-deleted as a whole, never leaving
        // orphaned diagnosis/investigation lines behind. UsePropertyAccessMode(Field) is
        // required because Diagnoses/Investigations are read-only computed properties backed by
        // private fields, not settable auto-properties.
        builder.HasMany(x => x.Diagnoses)
            .WithOne()
            .HasForeignKey(d => d.OpdConsultationNoteId)
            .HasConstraintName("fk_opd_consultation_diagnoses_note_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Diagnoses).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Investigations)
            .WithOne()
            .HasForeignKey(i => i.OpdConsultationNoteId)
            .HasConstraintName("fk_opd_consultation_investigations_note_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Investigations).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Prescriptions)
            .WithOne()
            .HasForeignKey(p => p.OpdConsultationNoteId)
            .HasConstraintName("fk_opd_consultation_prescriptions_note_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Prescriptions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal class OpdConsultationDiagnosisConfiguration : IEntityTypeConfiguration<OpdConsultationDiagnosis>
{
    public void Configure(EntityTypeBuilder<OpdConsultationDiagnosis> builder)
    {
        builder.ToTable("opd_consultation_diagnoses");

        builder.HasKey(x => x.Id).HasName("pk_opd_consultation_diagnoses");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.OpdConsultationNoteId).HasColumnName("opd_consultation_note_id").IsRequired();

        // App-level reference into Masters' Diagnosis catalog — no DB FK, validated in
        // OpdConsultationService (mirrors ConsultantConsultationType.ConsultationTypeId).
        builder.Property(x => x.DiagnosisId).HasColumnName("diagnosis_id").IsRequired();
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(x => x.OpdConsultationNoteId).HasDatabaseName("ix_opd_consultation_diagnoses_note_id");
        builder.HasIndex(x => x.DiagnosisId).HasDatabaseName("ix_opd_consultation_diagnoses_diagnosis_id");
    }
}

internal class OpdConsultationInvestigationConfiguration : IEntityTypeConfiguration<OpdConsultationInvestigation>
{
    public void Configure(EntityTypeBuilder<OpdConsultationInvestigation> builder)
    {
        builder.ToTable("opd_consultation_investigations");

        builder.HasKey(x => x.Id).HasName("pk_opd_consultation_investigations");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.OpdConsultationNoteId).HasColumnName("opd_consultation_note_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Department).HasColumnName("department").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Priority).HasColumnName("priority").HasConversion<string>().HasMaxLength(20).IsRequired();

        // App-level reference into Masters' DiagnosticService catalog — no DB FK (see
        // OpdConsultationInvestigation.ServiceId). Null for a free-text line.
        builder.Property(x => x.ServiceId).HasColumnName("service_id");

        builder.HasIndex(x => x.OpdConsultationNoteId).HasDatabaseName("ix_opd_consultation_investigations_note_id");
    }
}

internal class OpdConsultationPrescriptionConfiguration : IEntityTypeConfiguration<OpdConsultationPrescription>
{
    public void Configure(EntityTypeBuilder<OpdConsultationPrescription> builder)
    {
        builder.ToTable("opd_consultation_prescriptions");

        builder.HasKey(x => x.Id).HasName("pk_opd_consultation_prescriptions");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.OpdConsultationNoteId).HasColumnName("opd_consultation_note_id").IsRequired();
        builder.Property(x => x.DrugName).HasColumnName("drug_name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Dose).HasColumnName("dose").HasMaxLength(100);
        builder.Property(x => x.Route).HasColumnName("route").HasMaxLength(50);
        builder.Property(x => x.Frequency).HasColumnName("frequency").HasMaxLength(100);
        builder.Property(x => x.DurationDays).HasColumnName("duration_days");
        builder.Property(x => x.Instructions).HasColumnName("instructions").HasMaxLength(500);

        builder.HasIndex(x => x.OpdConsultationNoteId).HasDatabaseName("ix_opd_consultation_prescriptions_note_id");
    }
}
