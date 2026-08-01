using HMS.Modules.Products.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Products.Infrastructure;

/// <summary>
/// Owns the "products" PostgreSQL schema — the product/item catalog and its child records
/// (barcodes, batches, prices, images, dynamic attributes, tax mappings), per
/// docs/04_Product_Management_ERD. Per docs/DatabaseArchitecture.md §1, only this module's
/// own code constructs/migrates this context — no other module references it. Some of this
/// module's own tables carry foreign keys into the Masters module's schema (see
/// ProductConfiguration); those are modeled as plain columns, never as EF navigations into
/// Masters' internal domain types.
/// </summary>
public class ProductsDbContext : DbContext
{
    public const string SchemaName = "products";

    public ProductsDbContext(DbContextOptions<ProductsDbContext> options) : base(options)
    {
    }

    // Internal (not public): each entity is an internal domain type, so public DbSet<T>
    // properties would be CS0053 accessibility violations. The context itself stays public
    // (HMS.Api's Program.cs resolves it by type for the startup migration call), but these
    // DbSets are only ever queried from within this module.
    internal DbSet<Product> Products => Set<Product>();
    internal DbSet<ProductBarcode> ProductBarcodes => Set<ProductBarcode>();
    internal DbSet<ProductBatch> ProductBatches => Set<ProductBatch>();
    internal DbSet<ProductPrice> ProductPrices => Set<ProductPrice>();
    internal DbSet<ProductImage> ProductImages => Set<ProductImage>();
    internal DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
    internal DbSet<ProductAttributeValue> ProductAttributeValues => Set<ProductAttributeValue>();
    internal DbSet<ProductTaxMapping> ProductTaxMappings => Set<ProductTaxMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductsDbContext).Assembly);
    }
}
