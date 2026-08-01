using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

internal class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.ToTable("units_of_measure");

        builder.HasKey(u => u.Id).HasName("pk_units_of_measure");
        builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(u => u.UomCode).HasColumnName("uom_code").HasMaxLength(20).IsRequired();
        builder.Property(u => u.UomName).HasColumnName("uom_name").HasMaxLength(100).IsRequired();
        builder.Property(u => u.UomType).HasColumnName("uom_type").HasMaxLength(20);
        builder.Property(u => u.IsBaseUnit).HasColumnName("is_base_unit").IsRequired().HasDefaultValue(false);
        builder.Property(u => u.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(u => u.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(u => u.CreatedBy).HasColumnName("created_by");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");
        builder.Property(u => u.UpdatedBy).HasColumnName("updated_by");
        builder.Property(u => u.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(u => u.DeletedAt).HasColumnName("deleted_at");
        builder.Property(u => u.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(u => !u.IsDeleted);

        builder.HasIndex(u => u.UomCode).IsUnique().HasDatabaseName("ux_units_of_measure_uom_code").HasFilter("is_deleted = false");
    }
}
