using HMS.Modules.HR.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.HR.Infrastructure.Configurations;

internal class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.ToTable("attendances");

        builder.HasKey(a => a.Id).HasName("pk_attendances");
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.EmployeeId).HasColumnName("employee_id").IsRequired();
        builder.Property(a => a.AttendanceDate).HasColumnName("attendance_date").HasColumnType("date").IsRequired();
        builder.Property(a => a.CheckInTime).HasColumnName("check_in_time");
        builder.Property(a => a.CheckOutTime).HasColumnName("check_out_time");
        builder.Property(a => a.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.Remarks).HasColumnName("remarks").HasMaxLength(500);

        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(a => a.CreatedBy).HasColumnName("created_by");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");
        builder.Property(a => a.UpdatedBy).HasColumnName("updated_by");
        builder.Property(a => a.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(a => a.DeletedAt).HasColumnName("deleted_at");
        builder.Property(a => a.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(a => !a.IsDeleted);

        // One attendance row per employee per day.
        builder.HasIndex(a => new { a.EmployeeId, a.AttendanceDate }).IsUnique().HasDatabaseName("ux_attendances_employee_id_attendance_date").HasFilter("is_deleted = false");
        builder.HasIndex(a => a.AttendanceDate).HasDatabaseName("ix_attendances_attendance_date");

        // Real FK — Attendance and Employee both live in the "hr" schema (see Attendance's
        // own remarks). Restrict: an employee record is soft-deleted, never hard-deleted, so
        // there's no hard-delete path this would guard against, same conservative default as
        // ShiftAssignment's FK to Shift.
        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .HasConstraintName("fk_attendances_employees_employee_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
