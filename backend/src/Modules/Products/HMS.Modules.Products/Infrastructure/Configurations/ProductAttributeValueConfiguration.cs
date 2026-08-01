using HMS.Modules.Products.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Products.Infrastructure.Configurations;

internal class ProductAttributeValueConfiguration : IEntityTypeConfiguration<ProductAttributeValue>
{
    public void Configure(EntityTypeBuilder<ProductAttributeValue> builder)
    {
        builder.ToTable("product_attribute_values");

        builder.HasKey(v => v.Id).HasName("pk_product_attribute_values");
        builder.Property(v => v.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(v => v.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(v => v.AttributeId).HasColumnName("attribute_id").IsRequired();
        builder.Property(v => v.AttributeValue).HasColumnName("attribute_value").IsRequired();
        builder.Property(v => v.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(v => v.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(v => v.CreatedBy).HasColumnName("created_by");
        builder.Property(v => v.UpdatedAt).HasColumnName("updated_at");
        builder.Property(v => v.UpdatedBy).HasColumnName("updated_by");
        builder.Property(v => v.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(v => v.DeletedAt).HasColumnName("deleted_at");
        builder.Property(v => v.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(v => !v.IsDeleted);

        builder.HasIndex(v => new { v.ProductId, v.AttributeId }).IsUnique().HasDatabaseName("ux_product_attribute_values_product_id_attribute_id").HasFilter("is_deleted = false");
        builder.HasIndex(v => new { v.AttributeId, v.AttributeValue }).HasDatabaseName("ix_product_attribute_values_attribute_id_attribute_value");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(v => v.ProductId)
            .HasConstraintName("fk_product_attribute_values_products_product_id")
            .OnDelete(DeleteBehavior.Restrict);

        // Same-schema real FK — ProductAttribute is a public-within-module domain type here.
        builder.HasOne<ProductAttribute>()
            .WithMany()
            .HasForeignKey(v => v.AttributeId)
            .HasConstraintName("fk_product_attribute_values_product_attributes_attribute_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
