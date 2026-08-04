using HMS.Modules.HR.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.HR.Infrastructure.Configurations;

internal class StaffAvailabilityConfiguration : IEntityTypeConfiguration<StaffAvailability>
{
    public void Configure(EntityTypeBuilder<StaffAvailability> builder)
    {
        // Singular, matching the singular API route (/api/v1/staff-availability) — not the
        // usual pluralized table-name convention (shifts, shift_assignments).
        builder.ToTable("staff_availability");

        builder.HasKey(a => a.Id).HasName("pk_staff_availability");
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.StaffId).HasColumnName("staff_id").IsRequired();
        builder.Property(a => a.StartDate).HasColumnName("start_date").HasColumnType("date").IsRequired();
        builder.Property(a => a.EndDate).HasColumnName("end_date").HasColumnType("date").IsRequired();
        builder.Property(a => a.AvailabilityStatus).HasColumnName("availability_status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.Reason).HasColumnName("reason");

        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(a => a.CreatedBy).HasColumnName("created_by");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");
        builder.Property(a => a.UpdatedBy).HasColumnName("updated_by");
        builder.Property(a => a.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(a => a.DeletedAt).HasColumnName("deleted_at");
        builder.Property(a => a.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(a => !a.IsDeleted);

        builder.HasIndex(a => a.StaffId).HasDatabaseName("ix_staff_availability_staff_id");
        builder.HasIndex(a => new { a.StartDate, a.EndDate }).HasDatabaseName("ix_staff_availability_start_date_end_date");
    }
}
