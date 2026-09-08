using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.DischargeSummary.Infrastructure.Configurations;

/// <summary>
/// Maps <see cref="Domain.DischargeSummary"/> to discharge_summary.discharge_summaries,
/// following the naming/PK/audit-column/soft-delete standards in
/// docs/DatabaseArchitecture.md. Internal (not public): the entity is an internal domain
/// type, so a public Configure(EntityTypeBuilder&lt;DischargeSummary&gt;) member would be a
/// CS0051 accessibility violation. EF Core's ApplyConfigurationsFromAssembly discovers and
/// invokes this via reflection regardless of the type's own visibility, so internal is
/// sufficient here.
/// </summary>
internal class DischargeSummaryConfiguration : IEntityTypeConfiguration<Domain.DischargeSummary>
{
    public void Configure(EntityTypeBuilder<Domain.DischargeSummary> builder)
    {
        builder.ToTable("discharge_summaries");

        builder.HasKey(x => x.Id).HasName("pk_discharge_summaries");
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever(); // Generated in the domain (DischargeSummary.Create), not by the database.

        // No FK constraint to ipd.admissions/patients.patients — cross-schema references are
        // a deliberate, reviewed exception (docs/DatabaseArchitecture.md §7), not a default;
        // mirrors HMS.Modules.Pharmacy.Infrastructure.Configurations.PharmacyStockTransaction
        // Configuration's identical treatment of PatientId/AdmissionId. Validated instead at
        // the Application layer via IAdmissionService/IPatientService.
        builder.Property(x => x.AdmissionId).HasColumnName("admission_id").IsRequired();
        builder.Property(x => x.PatientId).HasColumnName("patient_id").IsRequired();

        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(x => x.FinalDiagnosis).HasColumnName("final_diagnosis").HasMaxLength(2000);

        // Clinical.
        builder.Property(x => x.ChiefComplaints).HasColumnName("chief_complaints").HasMaxLength(2000);
        builder.Property(x => x.HistoryOfPresentingIllness).HasColumnName("history_of_presenting_illness").HasMaxLength(8000);
        builder.Property(x => x.PastMedicalHistory).HasColumnName("past_medical_history").HasMaxLength(2000);
        builder.Property(x => x.PastSurgicalHistory).HasColumnName("past_surgical_history").HasMaxLength(2000);
        builder.Property(x => x.FamilyHistory).HasColumnName("family_history").HasMaxLength(2000);
        builder.Property(x => x.PersonalHistory).HasColumnName("personal_history").HasMaxLength(2000);

        // Examination.
        builder.Property(x => x.GeneralExamination).HasColumnName("general_examination").HasMaxLength(2000);
        builder.Property(x => x.CvsFindings).HasColumnName("cvs_findings").HasMaxLength(1000);
        builder.Property(x => x.RsFindings).HasColumnName("rs_findings").HasMaxLength(1000);
        builder.Property(x => x.PaFindings).HasColumnName("pa_findings").HasMaxLength(1000);
        builder.Property(x => x.CnsFindings).HasColumnName("cns_findings").HasMaxLength(1000);
        builder.Property(x => x.LocalExamination).HasColumnName("local_examination").HasMaxLength(2000);
        builder.Property(x => x.Gait).HasColumnName("gait").HasMaxLength(500);

        // Vitals — a single snapshot, not a repeating observations table.
        builder.Property(x => x.HeightCm).HasColumnName("height_cm").HasColumnType("numeric(5,2)");
        builder.Property(x => x.WeightKg).HasColumnName("weight_kg").HasColumnType("numeric(5,2)");
        builder.Property(x => x.PulseRate).HasColumnName("pulse_rate");
        builder.Property(x => x.RespiratoryRate).HasColumnName("respiratory_rate");
        builder.Property(x => x.TemperatureF).HasColumnName("temperature_f").HasColumnType("numeric(5,2)");
        builder.Property(x => x.SpO2Percent).HasColumnName("spo2_percent");
        builder.Property(x => x.BloodPressure).HasColumnName("blood_pressure").HasMaxLength(20);

        builder.Property(x => x.CourseInHospital).HasColumnName("course_in_hospital").HasMaxLength(8000);

        // Surgical Details — plain manual fields (no OT module yet).
        builder.Property(x => x.ProcedureName).HasColumnName("procedure_name").HasMaxLength(500);
        builder.Property(x => x.ProcedureDateTime).HasColumnName("procedure_date_time");
        builder.Property(x => x.PrimarySurgeon).HasColumnName("primary_surgeon").HasMaxLength(500);
        builder.Property(x => x.AssistantSurgeons).HasColumnName("assistant_surgeons").HasMaxLength(1000);
        builder.Property(x => x.Anaesthetist).HasColumnName("anaesthetist").HasMaxLength(500);
        builder.Property(x => x.Anaesthesia).HasColumnName("anaesthesia").HasMaxLength(500);
        builder.Property(x => x.SurgicalPosition).HasColumnName("surgical_position").HasMaxLength(500);
        builder.Property(x => x.IntraOperativeFindings).HasColumnName("intra_operative_findings").HasMaxLength(8000);
        builder.Property(x => x.OperativeNotes).HasColumnName("operative_notes").HasMaxLength(8000);

        // Discharge Advice.
        builder.Property(x => x.Diet).HasColumnName("diet").HasMaxLength(2000);
        builder.Property(x => x.WoundCare).HasColumnName("wound_care").HasMaxLength(2000);
        builder.Property(x => x.Activity).HasColumnName("activity").HasMaxLength(2000);
        builder.Property(x => x.Physiotherapy).HasColumnName("physiotherapy").HasMaxLength(2000);
        builder.Property(x => x.ReviewInstructions).HasColumnName("review_instructions").HasMaxLength(2000);
        builder.Property(x => x.EmergencyInstructions).HasColumnName("emergency_instructions").HasMaxLength(2000);
        builder.Property(x => x.ConditionAtDischarge).HasColumnName("condition_at_discharge").HasMaxLength(2000);

        // Sign-off — opaque Guids into identity.users, same cross-schema-no-FK convention as
        // AdmissionId/PatientId above.
        builder.Property(x => x.PreparedByUserId).HasColumnName("prepared_by_user_id");
        builder.Property(x => x.CheckedByUserId).HasColumnName("checked_by_user_id");
        builder.Property(x => x.ConsultantApprovedByUserId).HasColumnName("consultant_approved_by_user_id");
        builder.Property(x => x.FinalizedAt).HasColumnName("finalized_at");
        builder.Property(x => x.FinalizedByUserId).HasColumnName("finalized_by_user_id");

        // Standard audit columns (docs/DatabaseArchitecture.md §5).
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");

        // Optimistic concurrency via Postgres's own system column — the Npgsql provider's
        // former UseXminAsConcurrencyToken() convenience method no longer exists in the
        // referenced 10.0.0 package (docs/DeveloperHandbook.md §20), so this is the
        // equivalent, still-supported manual mapping of the "xmin" system column.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .IsRowVersion();

        // Soft-deleted summaries are excluded from every query unless explicitly requested
        // (docs/DatabaseArchitecture.md §6).
        builder.HasQueryFilter(x => !x.IsDeleted);

        // One discharge summary per admission — the service layer already enforces this with
        // a check-then-create, this is the backstop per docs/DecisionLog.md.
        builder.HasIndex(x => x.AdmissionId)
            .IsUnique()
            .HasDatabaseName("ux_discharge_summaries_admission_id")
            .HasFilter("is_deleted = false");

        builder.HasIndex(x => x.PatientId).HasDatabaseName("ix_discharge_summaries_patient_id");
    }
}
