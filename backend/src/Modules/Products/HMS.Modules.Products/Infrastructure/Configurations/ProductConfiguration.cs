using HMS.Modules.Products.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Products.Infrastructure.Configurations;

internal class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id).HasName("pk_products");
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(p => p.Sku).HasColumnName("sku").HasMaxLength(50).IsRequired();
        builder.Property(p => p.ProductCode).HasColumnName("product_code").HasMaxLength(50).IsRequired();
        builder.Property(p => p.ProductName).HasColumnName("product_name").HasMaxLength(200).IsRequired();
        builder.Property(p => p.GenericName).HasColumnName("generic_name").HasMaxLength(200);
        builder.Property(p => p.Description).HasColumnName("description");

        // Cross-schema references into masters.* — scalar FK columns only, no EF navigation
        // (the target CLR types are internal to HMS.Modules.Masters). Each gets a supporting
        // index here; the actual FK constraint is added by hand in the migration (see
        // docs/DatabaseArchitecture.md §7 on cross-schema FKs being a deliberate decision).
        builder.Property(p => p.BrandId).HasColumnName("brand_id").IsRequired();
        builder.Property(p => p.ManufacturerId).HasColumnName("manufacturer_id").IsRequired();
        builder.Property(p => p.CategoryId).HasColumnName("category_id").IsRequired();
        builder.Property(p => p.SubCategoryId).HasColumnName("sub_category_id").IsRequired();
        builder.Property(p => p.GroupId).HasColumnName("group_id").IsRequired();
        builder.Property(p => p.UomId).HasColumnName("uom_id").IsRequired();
        builder.Property(p => p.BaseUomId).HasColumnName("base_uom_id").IsRequired();

        builder.Property(p => p.IsBatchTracked).HasColumnName("is_batch_tracked").IsRequired().HasDefaultValue(false);
        builder.Property(p => p.IsSerialized).HasColumnName("is_serialized").IsRequired().HasDefaultValue(false);
        builder.Property(p => p.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(p => p.ReorderLevel).HasColumnName("reorder_level").HasPrecision(18, 4).IsRequired().HasDefaultValue(0m);
        builder.Property(p => p.MinStockLevel).HasColumnName("min_stock_level").HasPrecision(18, 4).IsRequired().HasDefaultValue(0m);
        builder.Property(p => p.MaxStockLevel).HasColumnName("max_stock_level").HasPrecision(18, 4).IsRequired().HasDefaultValue(0m);
        builder.Property(p => p.Mrp).HasColumnName("mrp").HasPrecision(18, 4).IsRequired().HasDefaultValue(0m);
        builder.Property(p => p.CostPrice).HasColumnName("cost_price").HasPrecision(18, 4).IsRequired().HasDefaultValue(0m);
        builder.Property(p => p.SellingPrice).HasColumnName("selling_price").HasPrecision(18, 4).IsRequired().HasDefaultValue(0m);

        builder.Property(p => p.HsnCode).HasColumnName("hsn_code").HasMaxLength(50);
        builder.Property(p => p.Weight).HasColumnName("weight").HasPrecision(18, 4);
        builder.Property(p => p.Volume).HasColumnName("volume").HasPrecision(18, 4);

        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedBy).HasColumnName("created_by");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.UpdatedBy).HasColumnName("updated_by");
        builder.Property(p => p.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");
        builder.Property(p => p.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => p.Sku).IsUnique().HasDatabaseName("ux_products_sku").HasFilter("is_deleted = false");
        builder.HasIndex(p => p.ProductCode).IsUnique().HasDatabaseName("ux_products_product_code").HasFilter("is_deleted = false");
        builder.HasIndex(p => p.BrandId).HasDatabaseName("ix_products_brand_id");
        builder.HasIndex(p => p.ManufacturerId).HasDatabaseName("ix_products_manufacturer_id");
        builder.HasIndex(p => p.CategoryId).HasDatabaseName("ix_products_category_id");
        builder.HasIndex(p => p.SubCategoryId).HasDatabaseName("ix_products_sub_category_id");
        builder.HasIndex(p => p.GroupId).HasDatabaseName("ix_products_group_id");
        builder.HasIndex(p => p.UomId).HasDatabaseName("ix_products_uom_id");
        builder.HasIndex(p => p.BaseUomId).HasDatabaseName("ix_products_base_uom_id");
    }
}
