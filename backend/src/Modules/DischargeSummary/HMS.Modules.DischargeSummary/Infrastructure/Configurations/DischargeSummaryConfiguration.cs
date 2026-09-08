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
