using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Modules.Pharmacy.Infrastructure.Configurations;
using HMS.Modules.Products.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Pharmacy.Infrastructure.Repositories;

internal class PharmacyStockControlRepository : IPharmacyStockControlRepository
{
    private readonly PharmacyDbContext _dbContext;

    public PharmacyStockControlRepository(PharmacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAdjustmentAsync(PharmacyStockAdjustment adjustment, CancellationToken cancellationToken)
        => await _dbContext.StockAdjustments.AddAsync(adjustment, cancellationToken);

    public Task<PharmacyStockAdjustment?> GetAdjustmentAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.StockAdjustments.AsNoTracking().Include(a => a.Lines.OrderBy(l => l.LineNo)).FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<(PharmacyStockAdjustment Adjustment, int LineCount, decimal? Value)> Items, int TotalCount)> GetAdjustmentsPagedAsync(StockAdjustmentListQuery query, CancellationToken cancellationToken)
    {
        var adjustments = _dbContext.StockAdjustments.AsNoTracking();

        if (query.StoreId.HasValue) adjustments = adjustments.Where(a => a.StoreId == query.StoreId.Value);
        if (query.Reason.HasValue) adjustments = adjustments.Where(a => a.Reason == query.Reason.Value);
        if (query.FromDate.HasValue) adjustments = adjustments.Where(a => a.AdjustmentDate >= query.FromDate.Value);
        if (query.ToDate.HasValue) adjustments = adjustments.Where(a => a.AdjustmentDate <= query.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            adjustments = adjustments.Where(a => EF.Functions.ILike(a.AdjustmentNo, term) || a.Lines.Any(l => EF.Functions.ILike(l.ProductName, term)));
        }

        var totalCount = await adjustments.CountAsync(cancellationToken);
        var page = await adjustments
            .OrderByDescending(a => a.AdjustmentDate).ThenByDescending(a => a.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(a => new { Adjustment = a, LineCount = a.Lines.Count, Value = a.Lines.Sum(l => l.Quantity * l.UnitCost) })
            .ToListAsync(cancellationToken);

        return (page.Select(p => (p.Adjustment, p.LineCount, p.Value)).ToList(), totalCount);
    }

    public async Task AddTransferAsync(PharmacyStockTransfer transfer, CancellationToken cancellationToken)
        => await _dbContext.StockTransfers.AddAsync(transfer, cancellationToken);

    public Task<PharmacyStockTransfer?> GetTransferAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.StockTransfers.AsNoTracking().Include(t => t.Lines.OrderBy(l => l.LineNo)).FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<(PharmacyStockTransfer Transfer, int LineCount)> Items, int TotalCount)> GetTransfersPagedAsync(StockTransferListQuery query, CancellationToken cancellationToken)
    {
        var transfers = _dbContext.StockTransfers.AsNoTracking();

        if (query.StoreId.HasValue) transfers = transfers.Where(t => t.FromStoreId == query.StoreId.Value || t.ToStoreId == query.StoreId.Value);
        if (query.FromDate.HasValue) transfers = transfers.Where(t => t.TransferDate >= query.FromDate.Value);
        if (query.ToDate.HasValue) transfers = transfers.Where(t => t.TransferDate <= query.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            transfers = transfers.Where(t => EF.Functions.ILike(t.TransferNo, term) || t.Lines.Any(l => EF.Functions.ILike(l.ProductName, term)));
        }

        var totalCount = await transfers.CountAsync(cancellationToken);
        var page = await transfers
            .OrderByDescending(t => t.TransferDate).ThenByDescending(t => t.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(t => new { Transfer = t, LineCount = t.Lines.Count })
            .ToListAsync(cancellationToken);

        return (page.Select(p => (p.Transfer, p.LineCount)).ToList(), totalCount);
    }

    public async Task AddCountAsync(PharmacyStockCount count, CancellationToken cancellationToken)
        => await _dbContext.StockCounts.AddAsync(count, cancellationToken);

    public Task<PharmacyStockCount?> GetCountAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.StockCounts.Include(c => c.Lines).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> HasOpenCountAsync(Guid storeId, CancellationToken cancellationToken)
        => _dbContext.StockCounts.AnyAsync(c => c.StoreId == storeId && c.Status == StockCountStatus.Open, cancellationToken);

    public async Task<(IReadOnlyList<(PharmacyStockCount Count, int LineCount, int CountedLines)> Items, int TotalCount)> GetCountsPagedAsync(StockCountListQuery query, CancellationToken cancellationToken)
    {
        var counts = _dbContext.StockCounts.AsNoTracking();

        if (query.StoreId.HasValue) counts = counts.Where(c => c.StoreId == query.StoreId.Value);
        if (query.Status.HasValue) counts = counts.Where(c => c.Status == query.Status.Value);

        var totalCount = await counts.CountAsync(cancellationToken);
        var page = await counts
            .OrderByDescending(c => c.StartedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(c => new { Count = c, LineCount = c.Lines.Count, CountedLines = c.Lines.Count(l => l.CountedQuantity != null) })
            .ToListAsync(cancellationToken);

        return (page.Select(p => (p.Count, p.LineCount, p.CountedLines)).ToList(), totalCount);
    }

    public async Task<IReadOnlyList<StoreStockLine>> GetStockLinesAsync(Guid? storeId, CancellationToken cancellationToken)
    {
        var balances = _dbContext.StockBalances.AsNoTracking().Where(b => b.QuantityOnHand > 0);
        if (storeId.HasValue) balances = balances.Where(b => b.StoreId == storeId.Value);

        return await balances.Select(b => new StoreStockLine(b.StoreId, b.ProductId, b.ProductBatchId, b.QuantityOnHand)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, (Guid SupplierId, string SupplierName)>> GetBatchSuppliersAsync(IReadOnlyCollection<Guid> productBatchIds, CancellationToken cancellationToken)
    {
        if (productBatchIds.Count == 0)
        {
            return new Dictionary<Guid, (Guid, string)>();
        }

        var ids = productBatchIds.Distinct().ToList();
        var rows = await (
            from line in _dbContext.GoodsReceiptLines.AsNoTracking()
            join receipt in _dbContext.GoodsReceipts.AsNoTracking() on line.GoodsReceiptId equals receipt.Id
            where ids.Contains(line.ProductBatchId)
            orderby receipt.ReceivedDate descending, receipt.CreatedAt descending
            select new { line.ProductBatchId, receipt.SupplierId, receipt.SupplierName })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<Guid, (Guid, string)>();
        foreach (var row in rows)
        {
            result.TryAdd(row.ProductBatchId, (row.SupplierId, row.SupplierName));
        }

        return result;
    }

    public async Task<IReadOnlyList<ScheduleRegisterEntry>> GetScheduleRegisterAsync(DateOnly fromDate, DateOnly toDate, IReadOnlyCollection<DrugSchedule> schedules, Guid? storeId, CancellationToken cancellationToken)
    {
        var fromUtc = PharmacyClock.StartOfDayUtc(fromDate);
        var toUtc = PharmacyClock.StartOfDayUtc(toDate.AddDays(1));
        var wanted = schedules.ToList();

        var query =
            from line in _dbContext.SaleLines.AsNoTracking()
            join sale in _dbContext.Sales.AsNoTracking() on line.SaleId equals sale.Id
            where sale.SaleDate >= fromUtc && sale.SaleDate < toUtc && wanted.Contains(line.Schedule)
            select new { line, sale };

        if (storeId.HasValue)
        {
            query = query.Where(x => x.sale.StoreId == storeId.Value);
        }

        return await query
            .OrderBy(x => x.sale.SaleDate).ThenBy(x => x.line.LineNo)
            .Select(x => new ScheduleRegisterEntry
            {
                SaleDate = x.sale.SaleDate,
                SaleId = x.sale.Id,
                BillNo = x.sale.BillNo,
                PatientName = x.sale.CustomerName,
                PatientUhid = x.sale.PatientUhid,
                PatientPhone = x.sale.CustomerPhone,
                DoctorName = x.sale.DoctorName,
                ProductName = x.line.ProductName,
                GenericName = x.line.GenericName,
                Schedule = x.line.Schedule,
                BatchNo = x.line.BatchNo,
                ExpiryDate = x.line.ExpiryDate,
                Quantity = x.line.Quantity,
                ReturnedQuantity = x.line.ReturnedQuantity,
            })
            .ToListAsync(cancellationToken);
    }
}

internal class PharmacyStockAdjustmentConfiguration : IEntityTypeConfiguration<PharmacyStockAdjustment>
{
    public void Configure(EntityTypeBuilder<PharmacyStockAdjustment> builder)
    {
        builder.MapDocument("pharmacy_stock_adjustments");

        builder.HasIndex(a => a.AdjustmentNo).IsUnique().HasDatabaseName("ux_pharmacy_stock_adjustments_adjustment_no");
        builder.HasIndex(a => a.AdjustmentDate).HasDatabaseName("ix_pharmacy_stock_adjustments_adjustment_date");

        builder.HasOne<PharmacyStore>().WithMany().HasForeignKey(a => a.StoreId)
            .HasConstraintName("fk_pharmacy_stock_adjustments_pharmacy_stores_store_id").OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Lines).WithOne().HasForeignKey(l => l.AdjustmentId)
            .HasConstraintName("fk_pharmacy_stock_adjustment_lines_adjustment_id").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(a => a.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal class PharmacyStockAdjustmentLineConfiguration : IEntityTypeConfiguration<PharmacyStockAdjustmentLine>
{
    public void Configure(EntityTypeBuilder<PharmacyStockAdjustmentLine> builder)
    {
        builder.MapDocument("pharmacy_stock_adjustment_lines");
        builder.HasIndex(l => l.AdjustmentId).HasDatabaseName("ix_pharmacy_stock_adjustment_lines_adjustment_id");
    }
}

internal class PharmacyStockTransferConfiguration : IEntityTypeConfiguration<PharmacyStockTransfer>
{
    public void Configure(EntityTypeBuilder<PharmacyStockTransfer> builder)
    {
        builder.MapDocument("pharmacy_stock_transfers");

        builder.HasIndex(t => t.TransferNo).IsUnique().HasDatabaseName("ux_pharmacy_stock_transfers_transfer_no");
        builder.HasIndex(t => t.TransferDate).HasDatabaseName("ix_pharmacy_stock_transfers_transfer_date");

        builder.HasOne<PharmacyStore>().WithMany().HasForeignKey(t => t.FromStoreId)
            .HasConstraintName("fk_pharmacy_stock_transfers_from_store_id").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PharmacyStore>().WithMany().HasForeignKey(t => t.ToStoreId)
            .HasConstraintName("fk_pharmacy_stock_transfers_to_store_id").OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Lines).WithOne().HasForeignKey(l => l.TransferId)
            .HasConstraintName("fk_pharmacy_stock_transfer_lines_transfer_id").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(t => t.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal class PharmacyStockTransferLineConfiguration : IEntityTypeConfiguration<PharmacyStockTransferLine>
{
    public void Configure(EntityTypeBuilder<PharmacyStockTransferLine> builder)
    {
        builder.MapDocument("pharmacy_stock_transfer_lines");
        builder.HasIndex(l => l.TransferId).HasDatabaseName("ix_pharmacy_stock_transfer_lines_transfer_id");
    }
}

internal class PharmacyStockCountConfiguration : IEntityTypeConfiguration<PharmacyStockCount>
{
    public void Configure(EntityTypeBuilder<PharmacyStockCount> builder)
    {
        builder.MapDocument("pharmacy_stock_counts");

        builder.HasIndex(c => c.CountNo).IsUnique().HasDatabaseName("ux_pharmacy_stock_counts_count_no");
        builder.HasIndex(c => new { c.StoreId, c.Status }).HasDatabaseName("ix_pharmacy_stock_counts_store_status");

        builder.HasOne<PharmacyStore>().WithMany().HasForeignKey(c => c.StoreId)
            .HasConstraintName("fk_pharmacy_stock_counts_pharmacy_stores_store_id").OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Lines).WithOne().HasForeignKey(l => l.StockCountId)
            .HasConstraintName("fk_pharmacy_stock_count_lines_stock_count_id").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal class PharmacyStockCountLineConfiguration : IEntityTypeConfiguration<PharmacyStockCountLine>
{
    public void Configure(EntityTypeBuilder<PharmacyStockCountLine> builder)
    {
        builder.MapDocument("pharmacy_stock_count_lines");
        builder.HasIndex(l => l.StockCountId).HasDatabaseName("ix_pharmacy_stock_count_lines_stock_count_id");
    }
}
