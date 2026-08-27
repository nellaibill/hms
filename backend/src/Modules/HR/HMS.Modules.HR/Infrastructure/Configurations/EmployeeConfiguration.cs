using HMS.Modules.HR.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.HR.Infrastructure.Configurations;

internal class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");

        builder.HasKey(e => e.Id).HasName("pk_employees");
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(e => e.EmployeeCode).HasColumnName("employee_code").HasMaxLength(30).IsRequired();
        builder.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Gender).HasColumnName("gender").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.DateOfBirth).HasColumnName("date_of_birth").HasColumnType("date").IsRequired();
        builder.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20).IsRequired();
        builder.Property(e => e.Email).HasColumnName("email").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Address).HasColumnName("address").HasMaxLength(500).IsRequired();
        builder.Property(e => e.EmergencyContactName).HasColumnName("emergency_contact_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.EmergencyContactPhone).HasColumnName("emergency_contact_phone").HasMaxLength(20).IsRequired();
        builder.Property(e => e.DepartmentId).HasColumnName("department_id").IsRequired();
        builder.Property(e => e.DesignationId).HasColumnName("designation_id").IsRequired();
        builder.Property(e => e.EmployeeType).HasColumnName("employee_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.JoiningDate).HasColumnName("joining_date").HasColumnType("date").IsRequired();
        builder.Property(e => e.EmploymentStatus).HasColumnName("employment_status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.ReportingManagerId).HasColumnName("reporting_manager_id");
        builder.Property(e => e.ProfilePhotoUrl).HasColumnName("profile_photo_url").HasMaxLength(1000);
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => e.EmployeeCode).IsUnique().HasDatabaseName("ux_employees_employee_code").HasFilter("is_deleted = false");
        builder.HasIndex(e => e.DepartmentId).HasDatabaseName("ix_employees_department_id");
        builder.HasIndex(e => e.DesignationId).HasDatabaseName("ix_employees_designation_id");
        builder.HasIndex(e => e.UserId).HasDatabaseName("ix_employees_user_id");

        // Self-referencing FK, real (same table, same schema) — unlike DepartmentId/
        // DesignationId/UserId, which are cross-module Guid references with no DB-level FK
        // (see Employee's own doc comment). Restrict, not Cascade: deleting/reassigning a
        // manager should never silently cascade-delete their reports.
        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(e => e.ReportingManagerId)
            .HasConstraintName("fk_employees_employees_reporting_manager_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
