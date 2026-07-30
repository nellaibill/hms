using HMS.Modules.Products.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Products.Infrastructure.Configurations;

internal class ProductPriceConfiguration : IEntityTypeConfiguration<ProductPrice>
{
    public void Configure(EntityTypeBuilder<ProductPrice> builder)
    {
        builder.ToTable("product_prices");

        builder.HasKey(p => p.Id).HasName("pk_product_prices");
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(p => p.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(p => p.PriceType).HasColumnName("price_type").HasMaxLength(30).IsRequired();
        // Cross-schema reference into masters.currencies — scalar column + index only, FK
        // constraint added by hand in the migration. See ProductConfiguration's note.
        builder.Property(p => p.CurrencyId).HasColumnName("currency_id").IsRequired();
        builder.Property(p => p.Price).HasColumnName("price").HasPrecision(18, 4).IsRequired();
        builder.Property(p => p.EffectiveFrom).HasColumnName("effective_from").HasColumnType("date").IsRequired();
        builder.Property(p => p.EffectiveTo).HasColumnName("effective_to").HasColumnType("date");
        builder.Property(p => p.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedBy).HasColumnName("created_by");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.UpdatedBy).HasColumnName("updated_by");
        builder.Property(p => p.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");
        builder.Property(p => p.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => new { p.ProductId, p.PriceType, p.CurrencyId, p.EffectiveFrom })
            .IsUnique()
            .HasDatabaseName("ux_product_prices_product_id_price_type_currency_id_effective_from")
            .HasFilter("is_deleted = false");
        builder.HasIndex(p => new { p.ProductId, p.PriceType, p.EffectiveFrom }).HasDatabaseName("ix_product_prices_product_id_price_type_effective_from");
        builder.HasIndex(p => p.CurrencyId).HasDatabaseName("ix_product_prices_currency_id");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(p => p.ProductId)
            .HasConstraintName("fk_product_prices_products_product_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
