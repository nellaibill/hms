using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.IPD.Infrastructure.Configurations;

internal class MedicationOrderConfiguration : IEntityTypeConfiguration<MedicationOrder>
{
    public void Configure(EntityTypeBuilder<MedicationOrder> builder)
    {
        builder.ToTable("medication_orders");

        builder.HasKey(o => o.Id).HasName("pk_medication_orders");
        builder.Property(o => o.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(o => o.AdmissionId).HasColumnName("admission_id").IsRequired();
        builder.Property(o => o.DrugName).HasColumnName("drug_name").HasMaxLength(200).IsRequired();
        builder.Property(o => o.Dose).HasColumnName("dose").HasMaxLength(100).IsRequired();
        builder.Property(o => o.Route).HasColumnName("route").HasMaxLength(100).IsRequired();
        builder.Property(o => o.Frequency).HasColumnName("frequency").HasMaxLength(100).IsRequired();
        builder.Property(o => o.StartDate).HasColumnName("start_date").IsRequired();
        builder.Property(o => o.EndDate).HasColumnName("end_date");
        builder.Property(o => o.Instructions).HasColumnName("instructions").HasMaxLength(2000);
        builder.Property(o => o.OrderedAt).HasColumnName("ordered_at").IsRequired();
        builder.Property(o => o.OrderedByUserId).HasColumnName("ordered_by_user_id");
        builder.Property(o => o.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(o => o.DiscontinuedAt).HasColumnName("discontinued_at");
        builder.Property(o => o.DiscontinuedReason).HasColumnName("discontinued_reason").HasMaxLength(1000);

        builder.Property(o => o.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(o => o.CreatedBy).HasColumnName("created_by");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at");
        builder.Property(o => o.UpdatedBy).HasColumnName("updated_by");
        builder.Property(o => o.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(o => o.DeletedAt).HasColumnName("deleted_at");
        builder.Property(o => o.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(o => !o.IsDeleted);

        builder.HasIndex(o => o.AdmissionId).HasDatabaseName("ix_medication_orders_admission_id");

        // No navigation collection on Admission itself, same convention as every other IPD
        // child entity. Restrict (not Cascade): Admission uses soft-delete, not hard delete.
        builder.HasOne<Admission>()
            .WithMany()
            .HasForeignKey(o => o.AdmissionId)
            .HasConstraintName("fk_medication_orders_admissions_admission_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
