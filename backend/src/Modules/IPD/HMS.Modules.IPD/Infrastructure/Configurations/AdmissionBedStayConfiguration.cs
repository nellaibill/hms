using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.IPD.Infrastructure.Configurations;

internal class AdmissionBedStayConfiguration : IEntityTypeConfiguration<AdmissionBedStay>
{
    public void Configure(EntityTypeBuilder<AdmissionBedStay> builder)
    {
        builder.ToTable("admission_bed_stays");

        builder.HasKey(s => s.Id).HasName("pk_admission_bed_stays");
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.AdmissionId).HasColumnName("admission_id").IsRequired();
        builder.Property(s => s.BedId).HasColumnName("bed_id").IsRequired();
        builder.Property(s => s.FromDateTime).HasColumnName("from_date_time").IsRequired();
        builder.Property(s => s.ToDateTime).HasColumnName("to_date_time");
        builder.Property(s => s.DailyCharge).HasColumnName("daily_charge").HasColumnType("numeric(12,2)").IsRequired();

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.CreatedBy).HasColumnName("created_by");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");
        builder.Property(s => s.UpdatedBy).HasColumnName("updated_by");
        builder.Property(s => s.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(s => s.DeletedAt).HasColumnName("deleted_at");
        builder.Property(s => s.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.HasIndex(s => s.AdmissionId).HasDatabaseName("ix_admission_bed_stays_admission_id");

        builder.HasOne<Admission>()
            .WithMany()
            .HasForeignKey(s => s.AdmissionId)
            .HasConstraintName("fk_admission_bed_stays_admissions_admission_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
