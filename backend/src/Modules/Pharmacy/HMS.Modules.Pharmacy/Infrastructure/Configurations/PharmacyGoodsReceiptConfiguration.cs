using HMS.Modules.Pharmacy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Pharmacy.Infrastructure.Configurations;

internal class PharmacyGoodsReceiptConfiguration : IEntityTypeConfiguration<PharmacyGoodsReceipt>
{
    public void Configure(EntityTypeBuilder<PharmacyGoodsReceipt> builder)
    {
        builder.MapDocument("pharmacy_goods_receipts");

        builder.HasIndex(r => r.ReceiptNo).IsUnique().HasDatabaseName("ux_pharmacy_goods_receipts_receipt_no");
        builder.HasIndex(r => new { r.SupplierId, r.SupplierInvoiceNo }).HasDatabaseName("ix_pharmacy_goods_receipts_supplier_invoice");
        builder.HasIndex(r => r.ReceivedDate).HasDatabaseName("ix_pharmacy_goods_receipts_received_date");

        builder.HasOne<PharmacyStore>().WithMany().HasForeignKey(r => r.StoreId)
            .HasConstraintName("fk_pharmacy_goods_receipts_pharmacy_stores_store_id").OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Lines).WithOne().HasForeignKey(l => l.GoodsReceiptId)
            .HasConstraintName("fk_pharmacy_goods_receipt_lines_goods_receipt_id").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal class PharmacyGoodsReceiptLineConfiguration : IEntityTypeConfiguration<PharmacyGoodsReceiptLine>
{
    public void Configure(EntityTypeBuilder<PharmacyGoodsReceiptLine> builder)
    {
        builder.Ignore(l => l.LineTotal);
        builder.Ignore(l => l.ReceivedUnits);
        builder.Ignore(l => l.UnitCost);
        builder.MapDocument("pharmacy_goods_receipt_lines");

        builder.HasIndex(l => l.GoodsReceiptId).HasDatabaseName("ix_pharmacy_goods_receipt_lines_goods_receipt_id");
        builder.HasIndex(l => l.ProductId).HasDatabaseName("ix_pharmacy_goods_receipt_lines_product_id");
    }
}
