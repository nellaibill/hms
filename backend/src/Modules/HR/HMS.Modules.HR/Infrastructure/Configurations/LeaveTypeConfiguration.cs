using HMS.Modules.HR.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.HR.Infrastructure.Configurations;

internal class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.ToTable("leave_types");

        builder.HasKey(l => l.Id).HasName("pk_leave_types");
        builder.Property(l => l.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(l => l.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
        builder.Property(l => l.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(l => l.MaxDaysPerYear).HasColumnName("max_days_per_year");
        builder.Property(l => l.IsPaid).HasColumnName("is_paid").IsRequired().HasDefaultValue(false);
        builder.Property(l => l.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(l => l.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(l => l.CreatedBy).HasColumnName("created_by");
        builder.Property(l => l.UpdatedAt).HasColumnName("updated_at");
        builder.Property(l => l.UpdatedBy).HasColumnName("updated_by");
        builder.Property(l => l.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(l => l.DeletedAt).HasColumnName("deleted_at");
        builder.Property(l => l.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(l => !l.IsDeleted);

        builder.HasIndex(l => l.Code).IsUnique().HasDatabaseName("ux_leave_types_code").HasFilter("is_deleted = false");
    }
}
