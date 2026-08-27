using HMS.Modules.HR.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.HR.Infrastructure.Configurations;

internal class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("leave_requests");

        builder.HasKey(l => l.Id).HasName("pk_leave_requests");
        builder.Property(l => l.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(l => l.EmployeeId).HasColumnName("employee_id").IsRequired();
        builder.Property(l => l.LeaveTypeId).HasColumnName("leave_type_id").IsRequired();
        builder.Property(l => l.StartDate).HasColumnName("start_date").HasColumnType("date").IsRequired();
        builder.Property(l => l.EndDate).HasColumnName("end_date").HasColumnType("date").IsRequired();
        builder.Property(l => l.TotalDays).HasColumnName("total_days").IsRequired();
        builder.Property(l => l.Reason).HasColumnName("reason").HasMaxLength(500).IsRequired();
        builder.Property(l => l.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(l => l.ApprovedByUserId).HasColumnName("approved_by_user_id");
        builder.Property(l => l.ApprovedAt).HasColumnName("approved_at");
        builder.Property(l => l.DecisionNotes).HasColumnName("decision_notes").HasMaxLength(500);

        builder.Property(l => l.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(l => l.CreatedBy).HasColumnName("created_by");
        builder.Property(l => l.UpdatedAt).HasColumnName("updated_at");
        builder.Property(l => l.UpdatedBy).HasColumnName("updated_by");
        builder.Property(l => l.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(l => l.DeletedAt).HasColumnName("deleted_at");
        builder.Property(l => l.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(l => !l.IsDeleted);

        builder.HasIndex(l => l.EmployeeId).HasDatabaseName("ix_leave_requests_employee_id");
        builder.HasIndex(l => l.LeaveTypeId).HasDatabaseName("ix_leave_requests_leave_type_id");
        builder.HasIndex(l => l.Status).HasDatabaseName("ix_leave_requests_status");
        builder.HasIndex(l => l.StartDate).HasDatabaseName("ix_leave_requests_start_date");

        // Real FKs — LeaveRequest, Employee, and LeaveType all live in the "hr" schema (see
        // Attendance's own remarks on why this differs from Employee's cross-module
        // Department/Designation references). Restrict, not Cascade: neither Employee nor
        // LeaveType is ever hard-deleted (both soft-delete only), so there's no hard-delete
        // path this would guard against — same conservative default as every other FK in
        // this module.
        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(l => l.EmployeeId)
            .HasConstraintName("fk_leave_requests_employees_employee_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LeaveType>()
            .WithMany()
            .HasForeignKey(l => l.LeaveTypeId)
            .HasConstraintName("fk_leave_requests_leave_types_leave_type_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
