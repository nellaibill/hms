using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.IPD.Infrastructure.Configurations;

internal class ProgressNoteConfiguration : IEntityTypeConfiguration<ProgressNote>
{
    public void Configure(EntityTypeBuilder<ProgressNote> builder)
    {
        builder.ToTable("progress_notes");

        builder.HasKey(p => p.Id).HasName("pk_progress_notes");
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(p => p.AdmissionId).HasColumnName("admission_id").IsRequired();
        builder.Property(p => p.NoteDateTime).HasColumnName("note_date_time").IsRequired();

        builder.Property(p => p.ClinicalCondition).HasColumnName("clinical_condition").HasMaxLength(2000);
        builder.Property(p => p.Progress).HasColumnName("progress").HasMaxLength(2000);
        builder.Property(p => p.Diagnosis).HasColumnName("diagnosis").HasMaxLength(2000);
        builder.Property(p => p.Assessment).HasColumnName("assessment").HasMaxLength(2000);
        builder.Property(p => p.Plan).HasColumnName("plan").HasMaxLength(2000);
        builder.Property(p => p.Instructions).HasColumnName("instructions").HasMaxLength(2000);
        builder.Property(p => p.AuthorUserId).HasColumnName("author_user_id");

        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedBy).HasColumnName("created_by");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.UpdatedBy).HasColumnName("updated_by");
        builder.Property(p => p.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");
        builder.Property(p => p.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => p.AdmissionId).HasDatabaseName("ix_progress_notes_admission_id");

        // No navigation collection on Admission itself, same convention as AdmissionCharge.
        // Restrict (not Cascade): Admission uses soft-delete, not hard delete.
        builder.HasOne<Admission>()
            .WithMany()
            .HasForeignKey(p => p.AdmissionId)
            .HasConstraintName("fk_progress_notes_admissions_admission_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
