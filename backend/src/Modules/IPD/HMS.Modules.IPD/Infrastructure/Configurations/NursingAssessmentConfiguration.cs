using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.IPD.Infrastructure.Configurations;

internal class NursingAssessmentConfiguration : IEntityTypeConfiguration<NursingAssessment>
{
    public void Configure(EntityTypeBuilder<NursingAssessment> builder)
    {
        builder.ToTable("nursing_assessments");

        builder.HasKey(n => n.Id).HasName("pk_nursing_assessments");
        builder.Property(n => n.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(n => n.AdmissionId).HasColumnName("admission_id").IsRequired();
        builder.Property(n => n.AssessedAt).HasColumnName("assessed_at").IsRequired();

        builder.Property(n => n.GeneralCondition).HasColumnName("general_condition").HasMaxLength(500);
        builder.Property(n => n.ConsciousnessLevel).HasColumnName("consciousness_level").HasMaxLength(200);
        builder.Property(n => n.Mobility).HasColumnName("mobility").HasMaxLength(500);
        builder.Property(n => n.NutritionStatus).HasColumnName("nutrition_status").HasMaxLength(500);
        builder.Property(n => n.FallRisk).HasColumnName("fall_risk").HasMaxLength(200);
        builder.Property(n => n.PressureSoreRisk).HasColumnName("pressure_sore_risk").HasMaxLength(200);
        builder.Property(n => n.SkinCondition).HasColumnName("skin_condition").HasMaxLength(500);
        builder.Property(n => n.PainScore).HasColumnName("pain_score");
        builder.Property(n => n.Notes).HasColumnName("notes").HasMaxLength(1000);
        builder.Property(n => n.AssessedByUserId).HasColumnName("assessed_by_user_id");

        builder.Property(n => n.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(n => n.CreatedBy).HasColumnName("created_by");
        builder.Property(n => n.UpdatedAt).HasColumnName("updated_at");
        builder.Property(n => n.UpdatedBy).HasColumnName("updated_by");
        builder.Property(n => n.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(n => n.DeletedAt).HasColumnName("deleted_at");
        builder.Property(n => n.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(n => !n.IsDeleted);

        builder.HasIndex(n => n.AdmissionId).HasDatabaseName("ix_nursing_assessments_admission_id");

        // No navigation collection on Admission itself, same convention as AdmissionCharge.
        // Restrict (not Cascade): Admission uses soft-delete, not hard delete.
        builder.HasOne<Admission>()
            .WithMany()
            .HasForeignKey(n => n.AdmissionId)
            .HasConstraintName("fk_nursing_assessments_admissions_admission_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
