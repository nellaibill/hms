using HMS.Modules.Pharmacy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Pharmacy.Infrastructure.Configurations;

internal class PharmacyStockTransactionConfiguration : IEntityTypeConfiguration<PharmacyStockTransaction>
{
    public void Configure(EntityTypeBuilder<PharmacyStockTransaction> builder)
    {
        builder.ToTable("pharmacy_stock_transactions");

        builder.HasKey(t => t.Id).HasName("pk_pharmacy_stock_transactions");
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(t => t.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(t => t.ProductBatchId).HasColumnName("product_batch_id").IsRequired();
        builder.Property(t => t.TransactionType).HasColumnName("transaction_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.Quantity).HasColumnName("quantity").HasPrecision(18, 4).IsRequired();
        builder.Property(t => t.BalanceAfter).HasColumnName("balance_after").HasPrecision(18, 4).IsRequired();
        builder.Property(t => t.TransactionDate).HasColumnName("transaction_date").IsRequired();
        builder.Property(t => t.PatientId).HasColumnName("patient_id");
        builder.Property(t => t.AdmissionId).HasColumnName("admission_id");
        builder.Property(t => t.Remarks).HasColumnName("remarks").HasMaxLength(500);

        builder.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(t => t.CreatedBy).HasColumnName("created_by");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
        builder.Property(t => t.UpdatedBy).HasColumnName("updated_by");
        builder.Property(t => t.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");
        builder.Property(t => t.DeletedBy).HasColumnName("deleted_by");

        // UseXminAsConcurrencyToken() doesn't exist in the pinned Npgsql EF Core provider
        // version — manual mapping instead (docs/DeveloperHandbook.md §20). Ledger rows are
        // never updated after insert (no Update method on PharmacyStockTransaction), so this
        // token is mapped for consistency with every other Entity-derived configuration in
        // this codebase rather than because a concurrent-update race is expected here.
        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        // Mapped for consistency with the base Entity type (mirrors every other
        // Entity-derived configuration in this codebase, e.g. BedTransferHistoryConfiguration)
        // even though nothing in this module ever soft-deletes a ledger row — Domain
        // deliberately exposes no Update/Delete method on PharmacyStockTransaction, so this
        // filter is dormant in practice, not a statement that ledger rows are expected to be
        // removable.
        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.HasIndex(t => new { t.ProductId, t.ProductBatchId }).HasDatabaseName("ix_pharmacy_stock_transactions_product_batch");
        builder.HasIndex(t => t.TransactionDate).HasDatabaseName("ix_pharmacy_stock_transactions_transaction_date");
        builder.HasIndex(t => t.PatientId).HasDatabaseName("ix_pharmacy_stock_transactions_patient_id");
    }
}
