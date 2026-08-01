using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

internal class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("warehouses");

        builder.HasKey(w => w.Id).HasName("pk_warehouses");
        builder.Property(w => w.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(w => w.WarehouseCode).HasColumnName("warehouse_code").HasMaxLength(30).IsRequired();
        builder.Property(w => w.WarehouseName).HasColumnName("warehouse_name").HasMaxLength(150).IsRequired();
        builder.Property(w => w.Address).HasColumnName("address");
        builder.Property(w => w.Country).HasColumnName("country").HasMaxLength(100);
        builder.Property(w => w.State).HasColumnName("state").HasMaxLength(100);
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

        builder.HasIndex(w => w.WarehouseCode).IsUnique().HasDatabaseName("ux_warehouses_warehouse_code").HasFilter("is_deleted = false");
    }
}
