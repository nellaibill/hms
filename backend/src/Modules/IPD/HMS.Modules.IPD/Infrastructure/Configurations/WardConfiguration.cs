using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.IPD.Infrastructure.Configurations;

internal class WardConfiguration : IEntityTypeConfiguration<Ward>
{
    public void Configure(EntityTypeBuilder<Ward> builder)
    {
        builder.ToTable("wards");

        builder.HasKey(w => w.Id).HasName("pk_wards");
        builder.Property(w => w.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(w => w.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
        builder.Property(w => w.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(w => w.DepartmentId).HasColumnName("department_id").IsRequired();
        builder.Property(w => w.WardType).HasColumnName("ward_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(w => w.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(w => w.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(w => w.CreatedBy).HasColumnName("created_by");
        builder.Property(w => w.UpdatedAt).HasColumnName("updated_at");
        builder.Property(w => w.UpdatedBy).HasColumnName("updated_by");
        builder.Property(w => w.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(w => w.DeletedAt).HasColumnName("deleted_at");
        builder.Property(w => w.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(w => !w.IsDeleted);

        builder.HasIndex(w => w.Code).IsUnique().HasDatabaseName("ux_wards_code").HasFilter("is_deleted = false");
    }
}
