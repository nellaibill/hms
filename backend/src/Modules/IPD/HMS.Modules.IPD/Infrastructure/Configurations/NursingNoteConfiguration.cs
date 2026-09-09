using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.IPD.Infrastructure.Configurations;

internal class NursingNoteConfiguration : IEntityTypeConfiguration<NursingNote>
{
    public void Configure(EntityTypeBuilder<NursingNote> builder)
    {
        builder.ToTable("nursing_notes");

        builder.HasKey(n => n.Id).HasName("pk_nursing_notes");
        builder.Property(n => n.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(n => n.AdmissionId).HasColumnName("admission_id").IsRequired();
        builder.Property(n => n.NoteDateTime).HasColumnName("note_date_time").IsRequired();
        builder.Property(n => n.Shift).HasColumnName("shift").HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(n => n.Observation).HasColumnName("observation").HasMaxLength(2000);
        builder.Property(n => n.Intervention).HasColumnName("intervention").HasMaxLength(2000);
        builder.Property(n => n.PatientResponse).HasColumnName("patient_response").HasMaxLength(2000);
        builder.Property(n => n.Remarks).HasColumnName("remarks").HasMaxLength(2000);
        builder.Property(n => n.RecordedByUserId).HasColumnName("recorded_by_user_id");

        builder.Property(n => n.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(n => n.CreatedBy).HasColumnName("created_by");
        builder.Property(n => n.UpdatedAt).HasColumnName("updated_at");
        builder.Property(n => n.UpdatedBy).HasColumnName("updated_by");
        builder.Property(n => n.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(n => n.DeletedAt).HasColumnName("deleted_at");
        builder.Property(n => n.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(n => !n.IsDeleted);

        builder.HasIndex(n => n.AdmissionId).HasDatabaseName("ix_nursing_notes_admission_id");

        // No navigation collection on Admission itself, same convention as AdmissionCharge.
        // Restrict (not Cascade): Admission uses soft-delete, not hard delete.
        builder.HasOne<Admission>()
            .WithMany()
            .HasForeignKey(n => n.AdmissionId)
            .HasConstraintName("fk_nursing_notes_admissions_admission_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
