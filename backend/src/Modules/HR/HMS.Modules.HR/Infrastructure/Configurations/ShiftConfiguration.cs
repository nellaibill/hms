using HMS.Modules.HR.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.HR.Infrastructure.Configurations;

internal class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("shifts");

        builder.HasKey(s => s.Id).HasName("pk_shifts");
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(s => s.StartTime).HasColumnName("start_time").IsRequired();
        builder.Property(s => s.EndTime).HasColumnName("end_time").IsRequired();
        builder.Property(s => s.BreakMinutes).HasColumnName("break_minutes").IsRequired();
        builder.Property(s => s.GraceMinutes).HasColumnName("grace_minutes").IsRequired();
        builder.Property(s => s.IsNightShift).HasColumnName("is_night_shift").IsRequired().HasDefaultValue(false);
        builder.Property(s => s.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.CreatedBy).HasColumnName("created_by");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");
        builder.Property(s => s.UpdatedBy).HasColumnName("updated_by");
        builder.Property(s => s.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(s => s.DeletedAt).HasColumnName("deleted_at");
        builder.Property(s => s.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.HasIndex(s => s.Code).IsUnique().HasDatabaseName("ux_shifts_code").HasFilter("is_deleted = false");
    }
}
