using HMS.Modules.Radiology.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Radiology.Infrastructure.Configurations;

/// <summary>Maps <see cref="XrayAiAnalysis"/> to radiology.xray_ai_analyses, following the
/// naming/PK/audit-column/soft-delete standards in docs/DatabaseArchitecture.md. Internal for the
/// same CS0051 reason as every other module's configurations.</summary>
internal class XrayAiAnalysisConfiguration : IEntityTypeConfiguration<XrayAiAnalysis>
{
    public void Configure(EntityTypeBuilder<XrayAiAnalysis> builder)
    {
        builder.ToTable("xray_ai_analyses");

        builder.HasKey(x => x.Id).HasName("pk_xray_ai_analyses");
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever(); // Generated in the domain (XrayAiAnalysis.Create), not by the database.

        builder.Property(x => x.PatientId).HasColumnName("patient_id").IsRequired();
        builder.Property(x => x.DocumentId).HasColumnName("document_id").IsRequired();
        builder.Property(x => x.Analysis).HasColumnName("analysis").IsRequired();
        builder.Property(x => x.Model).HasColumnName("model").HasMaxLength(200).IsRequired();

        builder.Property(x => x.IsReviewed).HasColumnName("is_reviewed").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
        builder.Property(x => x.ReviewedAtUtc).HasColumnName("reviewed_at_utc");

        // Standard audit columns (docs/DatabaseArchitecture.md §5).
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.PatientId).HasDatabaseName("ix_xray_ai_analyses_patient_id");
        builder.HasIndex(x => x.DocumentId).HasDatabaseName("ix_xray_ai_analyses_document_id");
    }
}
