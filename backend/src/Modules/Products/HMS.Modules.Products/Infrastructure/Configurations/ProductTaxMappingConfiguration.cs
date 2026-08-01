using HMS.Modules.Products.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Products.Infrastructure.Configurations;

internal class ProductTaxMappingConfiguration : IEntityTypeConfiguration<ProductTaxMapping>
{
    public void Configure(EntityTypeBuilder<ProductTaxMapping> builder)
    {
        builder.ToTable("product_tax_mappings");

        builder.HasKey(t => t.Id).HasName("pk_product_tax_mappings");
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(t => t.ProductId).HasColumnName("product_id").IsRequired();
        // Cross-schema reference into masters.taxes — scalar column + index only, FK
        // constraint added by hand in the migration. See ProductConfiguration's note.
        builder.Property(t => t.TaxId).HasColumnName("tax_id").IsRequired();
        builder.Property(t => t.TaxType).HasColumnName("tax_type").HasMaxLength(20).IsRequired();
        builder.Property(t => t.IsInclusive).HasColumnName("is_inclusive").IsRequired().HasDefaultValue(false);
        builder.Property(t => t.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(t => t.CreatedBy).HasColumnName("created_by");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
        builder.Property(t => t.UpdatedBy).HasColumnName("updated_by");
        builder.Property(t => t.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");
        builder.Property(t => t.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.HasIndex(t => new { t.ProductId, t.TaxId, t.TaxType }).IsUnique().HasDatabaseName("ux_product_tax_mappings_product_id_tax_id_tax_type").HasFilter("is_deleted = false");
        builder.HasIndex(t => t.TaxId).HasDatabaseName("ix_product_tax_mappings_tax_id");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(t => t.ProductId)
            .HasConstraintName("fk_product_tax_mappings_products_product_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
