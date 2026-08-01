using HMS.Modules.Products.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Products.Infrastructure.Configurations;

internal class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("product_images");

        builder.HasKey(i => i.Id).HasName("pk_product_images");
        builder.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(i => i.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(i => i.ImageUrl).HasColumnName("image_url").HasMaxLength(500).IsRequired();
        builder.Property(i => i.ImageType).HasColumnName("image_type").HasMaxLength(20).IsRequired();
        builder.Property(i => i.IsPrimary).HasColumnName("is_primary").IsRequired().HasDefaultValue(false);
        builder.Property(i => i.DisplayOrder).HasColumnName("display_order").IsRequired().HasDefaultValue(0);
        builder.Property(i => i.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(i => i.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(i => i.CreatedBy).HasColumnName("created_by");
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at");
        builder.Property(i => i.UpdatedBy).HasColumnName("updated_by");
        builder.Property(i => i.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(i => i.DeletedAt).HasColumnName("deleted_at");
        builder.Property(i => i.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(i => !i.IsDeleted);

        builder.HasIndex(i => new { i.ProductId, i.ImageType, i.DisplayOrder })
            .IsUnique()
            .HasDatabaseName("ux_product_images_product_id_image_type_display_order")
            .HasFilter("is_deleted = false");
        builder.HasIndex(i => i.ProductId).HasDatabaseName("ix_product_images_product_id");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .HasConstraintName("fk_product_images_products_product_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
