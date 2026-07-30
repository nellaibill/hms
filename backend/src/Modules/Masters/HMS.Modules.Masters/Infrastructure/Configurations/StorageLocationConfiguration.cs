using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

internal class StorageLocationConfiguration : IEntityTypeConfiguration<StorageLocation>
{
    public void Configure(EntityTypeBuilder<StorageLocation> builder)
    {
        builder.ToTable("storage_locations");

        builder.HasKey(s => s.Id).HasName("pk_storage_locations");
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.WarehouseId).HasColumnName("warehouse_id").IsRequired();
        builder.Property(s => s.LocationCode).HasColumnName("location_code").HasMaxLength(50).IsRequired();
        builder.Property(s => s.LocationName).HasColumnName("location_name").HasMaxLength(150).IsRequired();
        builder.Property(s => s.LocationType).HasColumnName("location_type").HasMaxLength(20);
        builder.Property(s => s.ParentLocationId).HasColumnName("parent_location_id");
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

        builder.HasIndex(s => new { s.WarehouseId, s.LocationCode }).IsUnique().HasDatabaseName("ux_storage_locations_warehouse_id_location_code").HasFilter("is_deleted = false");
        builder.HasIndex(s => s.ParentLocationId).HasDatabaseName("ix_storage_locations_parent_location_id");

        builder.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(s => s.WarehouseId)
            .HasConstraintName("fk_storage_locations_warehouse_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<StorageLocation>()
            .WithMany()
            .HasForeignKey(s => s.ParentLocationId)
            .HasConstraintName("fk_storage_locations_parent_location_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
