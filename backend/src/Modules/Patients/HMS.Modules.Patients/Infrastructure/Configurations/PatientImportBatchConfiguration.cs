using HMS.Modules.Patients.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Patients.Infrastructure.Configurations;

internal class PatientImportBatchConfiguration : IEntityTypeConfiguration<PatientImportBatch>
{
    public void Configure(EntityTypeBuilder<PatientImportBatch> builder)
    {
        builder.ToTable("patient_import_batches");

        builder.HasKey(b => b.Id).HasName("pk_patient_import_batches");
        builder.Property(b => b.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(b => b.FileName).HasColumnName("file_name").HasMaxLength(260).IsRequired();
        builder.Property(b => b.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(b => b.TotalRows).HasColumnName("total_rows").IsRequired().HasDefaultValue(0);
        builder.Property(b => b.ValidRows).HasColumnName("valid_rows").IsRequired().HasDefaultValue(0);
        builder.Property(b => b.InvalidRows).HasColumnName("invalid_rows").IsRequired().HasDefaultValue(0);
        builder.Property(b => b.CreatedRows).HasColumnName("created_rows").IsRequired().HasDefaultValue(0);
        builder.Property(b => b.CommitFailedRows).HasColumnName("commit_failed_rows").IsRequired().HasDefaultValue(0);
        builder.Property(b => b.CommittedBy).HasColumnName("committed_by");
        builder.Property(b => b.CommittedAt).HasColumnName("committed_at");

        // Standard audit columns. CreatedAt/CreatedBy double as "uploaded at/by" — see
        // ImportBatchResponse's UploadedAt/UploadedBy mapping.
        builder.Property(b => b.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(b => b.CreatedBy).HasColumnName("created_by");
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at");
        builder.Property(b => b.UpdatedBy).HasColumnName("updated_by");
        builder.Property(b => b.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(b => b.DeletedAt).HasColumnName("deleted_at");
        builder.Property(b => b.DeletedBy).HasColumnName("deleted_by");

        builder.HasQueryFilter(b => !b.IsDeleted);

        // Import History is always sorted newest-first.
        builder.HasIndex(b => b.CreatedAt).HasDatabaseName("ix_patient_import_batches_created_at");
    }
}
