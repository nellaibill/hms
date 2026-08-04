using HMS.Modules.HR.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.HR.Infrastructure.Configurations;

internal class ShiftAssignmentConfiguration : IEntityTypeConfiguration<ShiftAssignment>
{
    public void Configure(EntityTypeBuilder<ShiftAssignment> builder)
    {
        builder.ToTable("shift_assignments");

        builder.HasKey(sa => sa.Id).HasName("pk_shift_assignments");
        builder.Property(sa => sa.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(sa => sa.StaffId).HasColumnName("staff_id").IsRequired();
        builder.Property(sa => sa.DepartmentId).HasColumnName("department_id").IsRequired();
        builder.Property(sa => sa.ShiftId).HasColumnName("shift_id").IsRequired();
        builder.Property(sa => sa.RosterDate).HasColumnName("roster_date").HasColumnType("date").IsRequired();
        builder.Property(sa => sa.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(sa => sa.Remarks).HasColumnName("remarks");

        builder.Property(sa => sa.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(sa => sa.CreatedBy).HasColumnName("created_by");
        builder.Property(sa => sa.UpdatedAt).HasColumnName("updated_at");
        builder.Property(sa => sa.UpdatedBy).HasColumnName("updated_by");
        builder.Property(sa => sa.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(sa => sa.DeletedAt).HasColumnName("deleted_at");
        builder.Property(sa => sa.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(sa => !sa.IsDeleted);

        builder.HasIndex(sa => sa.StaffId).HasDatabaseName("ix_shift_assignments_staff_id");
        builder.HasIndex(sa => sa.RosterDate).HasDatabaseName("ix_shift_assignments_roster_date");

        // One Shift can have many ShiftAssignments. No navigation collection on Shift
        // itself — mirrors HMS.Modules.Products.ProductBatch's FK to Product, which
        // likewise adds no collection to Product. Restrict (not Cascade): Shift uses
        // soft-delete, not hard delete, so there is no hard-delete path this would guard
        // against — Restrict is the same conservative default ProductBatch uses.
        builder.HasOne<Shift>()
            .WithMany()
            .HasForeignKey(sa => sa.ShiftId)
            .HasConstraintName("fk_shift_assignments_shifts_shift_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
