using HMS.Modules.HR.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.HR.Infrastructure.Configurations;

internal class WeeklyRosterConfiguration : IEntityTypeConfiguration<WeeklyRoster>
{
    public void Configure(EntityTypeBuilder<WeeklyRoster> builder)
    {
        builder.ToTable("weekly_rosters");

        builder.HasKey(w => w.Id).HasName("pk_weekly_rosters");
        builder.Property(w => w.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(w => w.WeekStartDate).HasColumnName("week_start_date").HasColumnType("date").IsRequired();
        builder.Property(w => w.DepartmentId).HasColumnName("department_id").IsRequired();
        builder.Property(w => w.Published).HasColumnName("published").IsRequired().HasDefaultValue(false);
        builder.Property(w => w.PublishedDate).HasColumnName("published_date");

        builder.Property(w => w.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(w => w.CreatedBy).HasColumnName("created_by");
        builder.Property(w => w.UpdatedAt).HasColumnName("updated_at");
        builder.Property(w => w.UpdatedBy).HasColumnName("updated_by");
        builder.Property(w => w.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(w => w.DeletedAt).HasColumnName("deleted_at");
        builder.Property(w => w.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(w => !w.IsDeleted);

        // No uniqueness constraint on (DepartmentId, WeekStartDate) — "one roster per
        // department per week" wasn't part of this phase's validation scope.
        builder.HasIndex(w => w.DepartmentId).HasDatabaseName("ix_weekly_rosters_department_id");
        builder.HasIndex(w => w.WeekStartDate).HasDatabaseName("ix_weekly_rosters_week_start_date");
    }
}
