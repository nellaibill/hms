using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.IPD.Infrastructure.Configurations;

internal class MedicationAdministrationConfiguration : IEntityTypeConfiguration<MedicationAdministration>
{
    public void Configure(EntityTypeBuilder<MedicationAdministration> builder)
    {
        builder.ToTable("medication_administrations");

        builder.HasKey(a => a.Id).HasName("pk_medication_administrations");
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.MedicationOrderId).HasColumnName("medication_order_id").IsRequired();
        builder.Property(a => a.ScheduledTime).HasColumnName("scheduled_time").IsRequired();
        builder.Property(a => a.WasGiven).HasColumnName("was_given").IsRequired();
        builder.Property(a => a.AdministeredAt).HasColumnName("administered_at");
        builder.Property(a => a.Reason).HasColumnName("reason").HasMaxLength(500);
        builder.Property(a => a.Remarks).HasColumnName("remarks").HasMaxLength(1000);
        builder.Property(a => a.RecordedByUserId).HasColumnName("recorded_by_user_id");

        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(a => a.CreatedBy).HasColumnName("created_by");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");
        builder.Property(a => a.UpdatedBy).HasColumnName("updated_by");
        builder.Property(a => a.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(a => a.DeletedAt).HasColumnName("deleted_at");
        builder.Property(a => a.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(a => !a.IsDeleted);

        builder.HasIndex(a => a.MedicationOrderId).HasDatabaseName("ix_medication_administrations_medication_order_id");

        // Real, same-module FK (unlike AdmissionId elsewhere) — mirrors DischargeMedication's
        // FK to its parent DischargeSummary. Restrict: MedicationOrder is soft-deleted, not
        // hard-deleted, so there's no hard-delete path this would guard against.
        builder.HasOne<MedicationOrder>()
            .WithMany()
            .HasForeignKey(a => a.MedicationOrderId)
            .HasConstraintName("fk_medication_administrations_medication_orders_medication_order_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
