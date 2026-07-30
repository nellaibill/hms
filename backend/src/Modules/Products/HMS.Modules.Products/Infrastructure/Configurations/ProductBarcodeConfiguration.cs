using HMS.Modules.Products.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Products.Infrastructure.Configurations;

internal class ProductBarcodeConfiguration : IEntityTypeConfiguration<ProductBarcode>
{
    public void Configure(EntityTypeBuilder<ProductBarcode> builder)
    {
        builder.ToTable("product_barcodes");

        builder.HasKey(b => b.Id).HasName("pk_product_barcodes");
        builder.Property(b => b.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(b => b.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(b => b.BarcodeType).HasColumnName("barcode_type").HasMaxLength(20).IsRequired();
        builder.Property(b => b.BarcodeValue).HasColumnName("barcode_value").HasMaxLength(100).IsRequired();
        builder.Property(b => b.IsPrimary).HasColumnName("is_primary").IsRequired().HasDefaultValue(false);
        builder.Property(b => b.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);
        builder.Property(b => b.Notes).HasColumnName("notes");

        builder.Property(b => b.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(b => b.CreatedBy).HasColumnName("created_by");
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at");
        builder.Property(b => b.UpdatedBy).HasColumnName("updated_by");
        builder.Property(b => b.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(b => b.DeletedAt).HasColumnName("deleted_at");
        builder.Property(b => b.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(b => !b.IsDeleted);

        // Business rule 2: barcode_value is unique across ALL products, not per-product.
        builder.HasIndex(b => b.BarcodeValue).IsUnique().HasDatabaseName("ux_product_barcodes_barcode_value").HasFilter("is_deleted = false");
        builder.HasIndex(b => b.ProductId).HasDatabaseName("ix_product_barcodes_product_id");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(b => b.ProductId)
            .HasConstraintName("fk_product_barcodes_products_product_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
