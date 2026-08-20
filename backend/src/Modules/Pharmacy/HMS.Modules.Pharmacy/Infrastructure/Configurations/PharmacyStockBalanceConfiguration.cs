using HMS.Modules.Pharmacy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Pharmacy.Infrastructure.Configurations;

internal class PharmacyStockBalanceConfiguration : IEntityTypeConfiguration<PharmacyStockBalance>
{
    public void Configure(EntityTypeBuilder<PharmacyStockBalance> builder)
    {
        builder.ToTable("pharmacy_stock_balances");

        builder.HasKey(b => b.Id).HasName("pk_pharmacy_stock_balances");
        builder.Property(b => b.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(b => b.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(b => b.ProductBatchId).HasColumnName("product_batch_id").IsRequired();
        // 18,4 precision matches Products' own stock-quantity fields (ReorderLevel/
        // MinStockLevel/MaxStockLevel in ProductConfiguration) for consistency across the
        // two modules' stock-quantity columns.
        builder.Property(b => b.QuantityOnHand).HasColumnName("quantity_on_hand").HasPrecision(18, 4).IsRequired().HasDefaultValue(0m);

        builder.Property(b => b.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(b => b.CreatedBy).HasColumnName("created_by");
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at");
        builder.Property(b => b.UpdatedBy).HasColumnName("updated_by");
        builder.Property(b => b.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(b => b.DeletedAt).HasColumnName("deleted_at");
        builder.Property(b => b.DeletedBy).HasColumnName("deleted_by");

        // UseXminAsConcurrencyToken() doesn't exist in the pinned Npgsql EF Core provider
        // version — manual mapping instead (docs/DeveloperHandbook.md §20). This is the
        // concurrency token DispenseService.CreateAsync's retry loop reacts to
        // (DbUpdateConcurrencyException) when two concurrent dispenses race against the same
        // product/batch.
        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(b => !b.IsDeleted);

        // One balance row per product/batch — StockReceiptService/DispenseService's
        // get-or-create lookup relies on this being unique.
        builder.HasIndex(b => new { b.ProductId, b.ProductBatchId })
            .IsUnique()
            .HasDatabaseName("ux_pharmacy_stock_balances_product_batch")
            .HasFilter("is_deleted = false");
    }
}
