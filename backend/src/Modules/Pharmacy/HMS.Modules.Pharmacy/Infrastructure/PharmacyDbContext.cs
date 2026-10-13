using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Pharmacy.Infrastructure;

/// <summary>
/// Public (not internal): resolved by type from HMS.Api's Program.cs for the startup-time
/// migration call — same reason IPDDbContext/ProductsDbContext stay public. Everything else
/// in this module is internal.
/// </summary>
public class PharmacyDbContext : DbContext
{
    public const string SchemaName = "pharmacy";

    public PharmacyDbContext(DbContextOptions<PharmacyDbContext> options) : base(options)
    {
    }

    // Internal (not public): domain types are internal, so DbSet<T> properties stay internal too.
    internal DbSet<PharmacyStore> Stores => Set<PharmacyStore>();
    internal DbSet<PharmacyStockBalance> StockBalances => Set<PharmacyStockBalance>();
    internal DbSet<PharmacyStockTransaction> StockTransactions => Set<PharmacyStockTransaction>();
    internal DbSet<PharmacyGoodsReceipt> GoodsReceipts => Set<PharmacyGoodsReceipt>();
    internal DbSet<PharmacyGoodsReceiptLine> GoodsReceiptLines => Set<PharmacyGoodsReceiptLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);

        // One sequence per printed-number series — see IPharmacyUnitOfWork.NextNumberAsync.
        foreach (var series in DocumentSeries.All)
        {
            modelBuilder.HasSequence<long>(series.SequenceName, SchemaName).StartsAt(1);
        }

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PharmacyDbContext).Assembly);
    }
}
