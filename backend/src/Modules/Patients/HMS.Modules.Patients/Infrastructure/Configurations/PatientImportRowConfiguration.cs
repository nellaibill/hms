using HMS.Modules.Patients.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Patients.Infrastructure.Configurations;

internal class PatientImportRowConfiguration : IEntityTypeConfiguration<PatientImportRow>
{
    public void Configure(EntityTypeBuilder<PatientImportRow> builder)
    {
        builder.ToTable("patient_import_rows");

        builder.HasKey(r => r.Id).HasName("pk_patient_import_rows");
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(r => r.BatchId).HasColumnName("batch_id").IsRequired();
        builder.Property(r => r.RowNumber).HasColumnName("row_number").IsRequired();
        builder.Property(r => r.RawDataJson).HasColumnName("raw_data").HasColumnType("jsonb").IsRequired();
        builder.Property(r => r.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.ErrorsJson).HasColumnName("errors").HasColumnType("jsonb");
        builder.Property(r => r.MappedRequestJson).HasColumnName("mapped_request").HasColumnType("jsonb");
        builder.Property(r => r.CreatedPatientId).HasColumnName("created_patient_id");

        // Standard audit columns.
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(r => r.CreatedBy).HasColumnName("created_by");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");
        builder.Property(r => r.UpdatedBy).HasColumnName("updated_by");
        builder.Property(r => r.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at");
        builder.Property(r => r.DeletedBy).HasColumnName("deleted_by");

        builder.HasQueryFilter(r => !r.IsDeleted);

        // The review UI's core query: "rows for batch X with status Y", paginated.
        builder.HasIndex(r => new { r.BatchId, r.Status }).HasDatabaseName("ix_patient_import_rows_batch_id_status");

        builder.HasOne<PatientImportBatch>()
            .WithMany()
            .HasForeignKey(r => r.BatchId)
            .HasConstraintName("fk_patient_import_rows_batch_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
