using HMS.Modules.Products.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Products.Infrastructure.Configurations;

internal class ProductBatchConfiguration : IEntityTypeConfiguration<ProductBatch>
{
    public void Configure(EntityTypeBuilder<ProductBatch> builder)
    {
        builder.ToTable("product_batches");

        builder.HasKey(b => b.Id).HasName("pk_product_batches");
        builder.Property(b => b.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(b => b.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(b => b.BatchNo).HasColumnName("batch_no").HasMaxLength(100).IsRequired();
        builder.Property(b => b.ManufactureDate).HasColumnName("manufacture_date").HasColumnType("date").IsRequired();
        builder.Property(b => b.ExpiryDate).HasColumnName("expiry_date").HasColumnType("date").IsRequired();
        builder.Property(b => b.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(b => b.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(b => b.CreatedBy).HasColumnName("created_by");
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at");
        builder.Property(b => b.UpdatedBy).HasColumnName("updated_by");
        builder.Property(b => b.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(b => b.DeletedAt).HasColumnName("deleted_at");
        builder.Property(b => b.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(b => !b.IsDeleted);

        // Business rule 3: batch_no unique per product.
        builder.HasIndex(b => new { b.ProductId, b.BatchNo }).IsUnique().HasDatabaseName("ux_product_batches_product_id_batch_no").HasFilter("is_deleted = false");
        builder.HasIndex(b => new { b.ProductId, b.ExpiryDate }).HasDatabaseName("ix_product_batches_product_id_expiry_date");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(b => b.ProductId)
            .HasConstraintName("fk_product_batches_products_product_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
