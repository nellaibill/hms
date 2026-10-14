using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Modules.Pharmacy.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Pharmacy.Infrastructure.Repositories;

internal class PharmacyProcurementRepository : IPharmacyProcurementRepository
{
    private readonly PharmacyDbContext _dbContext;

    public PharmacyProcurementRepository(PharmacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddOrderAsync(PharmacyPurchaseOrder order, CancellationToken cancellationToken)
        => await _dbContext.PurchaseOrders.AddAsync(order, cancellationToken);

    public Task<PharmacyPurchaseOrder?> GetOrderAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.PurchaseOrders.Include(o => o.Lines.OrderBy(l => l.LineNo)).FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<(PharmacyPurchaseOrder Order, int LineCount, decimal Amount)> Items, int TotalCount)> GetOrdersPagedAsync(PurchaseOrderListQuery query, CancellationToken cancellationToken)
    {
        var orders = _dbContext.PurchaseOrders.AsNoTracking();

        if (query.SupplierId.HasValue)
        {
            orders = orders.Where(o => o.SupplierId == query.SupplierId.Value);
        }

        if (query.Status.HasValue)
        {
            orders = orders.Where(o => o.Status == query.Status.Value);
        }

        if (query.OpenOnly == true)
        {
            orders = orders.Where(o => o.Status == PurchaseOrderStatus.Ordered || o.Status == PurchaseOrderStatus.PartlyReceived);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            orders = orders.Where(o => EF.Functions.ILike(o.OrderNo, term) || EF.Functions.ILike(o.SupplierName, term));
        }

        var totalCount = await orders.CountAsync(cancellationToken);
        var page = await orders
            .OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(o => new { Order = o, LineCount = o.Lines.Count, Amount = o.Lines.Sum(l => l.Quantity * l.ExpectedRate) })
            .ToListAsync(cancellationToken);

        return (page.Select(p => (p.Order, p.LineCount, p.Amount)).ToList(), totalCount);
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetOnOrderQuantitiesAsync(Guid? storeId, CancellationToken cancellationToken)
    {
        var open = _dbContext.PurchaseOrders.AsNoTracking()
            .Where(o => o.Status == PurchaseOrderStatus.Ordered || o.Status == PurchaseOrderStatus.PartlyReceived);

        if (storeId.HasValue)
        {
            open = open.Where(o => o.StoreId == storeId.Value);
        }

        var pending = await open
            .SelectMany(o => o.Lines)
            .Where(l => l.Quantity > l.ReceivedQuantity)
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Pending = g.Sum(l => l.Quantity - l.ReceivedQuantity) })
            .ToListAsync(cancellationToken);

        return pending.ToDictionary(p => p.ProductId, p => p.Pending);
    }

    public async Task<IReadOnlyDictionary<Guid, LastPurchase>> GetLastPurchasesAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, LastPurchase>();
        }

        var ids = productIds.Distinct().ToList();

        // Every receipt line of these products with its header, newest first; the first one
        // seen per product is its last purchase. A pharmacy re-buys a few hundred products,
        // so this stays small — and it avoids a per-product "latest row" subquery.
        var rows = await (
            from line in _dbContext.GoodsReceiptLines.AsNoTracking()
            join receipt in _dbContext.GoodsReceipts.AsNoTracking() on line.GoodsReceiptId equals receipt.Id
            where ids.Contains(line.ProductId) && line.Quantity > 0
            orderby receipt.ReceivedDate descending, receipt.CreatedAt descending
            select new { line.ProductId, receipt.SupplierId, receipt.SupplierName, line.PurchaseRate, receipt.ReceivedDate })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<Guid, LastPurchase>();
        foreach (var row in rows)
        {
            result.TryAdd(row.ProductId, new LastPurchase(row.ProductId, row.SupplierId, row.SupplierName, row.PurchaseRate, row.ReceivedDate));
        }

        return result;
    }

    public async Task AddReturnAsync(PharmacyPurchaseReturn purchaseReturn, CancellationToken cancellationToken)
        => await _dbContext.PurchaseReturns.AddAsync(purchaseReturn, cancellationToken);

    public Task<PharmacyPurchaseReturn?> GetReturnAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.PurchaseReturns.Include(r => r.Lines.OrderBy(l => l.LineNo)).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<(PharmacyPurchaseReturn Return, int LineCount)> Items, int TotalCount)> GetReturnsPagedAsync(PurchaseReturnListQuery query, CancellationToken cancellationToken)
    {
        var returns = _dbContext.PurchaseReturns.AsNoTracking();

        if (query.SupplierId.HasValue)
        {
            returns = returns.Where(r => r.SupplierId == query.SupplierId.Value);
        }

        if (query.FromDate.HasValue)
        {
            returns = returns.Where(r => r.ReturnDate >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            returns = returns.Where(r => r.ReturnDate <= query.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            returns = returns.Where(r => EF.Functions.ILike(r.ReturnNo, term) || EF.Functions.ILike(r.SupplierName, term) || (r.CreditNoteNo != null && EF.Functions.ILike(r.CreditNoteNo, term)));
        }

        var totalCount = await returns.CountAsync(cancellationToken);
        var page = await returns
            .OrderByDescending(r => r.ReturnDate).ThenByDescending(r => r.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(r => new { Return = r, LineCount = r.Lines.Count })
            .ToListAsync(cancellationToken);

        return (page.Select(p => (p.Return, p.LineCount)).ToList(), totalCount);
    }

    public async Task AddPaymentAsync(PharmacySupplierPayment payment, CancellationToken cancellationToken)
        => await _dbContext.SupplierPayments.AddAsync(payment, cancellationToken);

    public async Task<IReadOnlyList<SupplierTotals>> GetSupplierTotalsAsync(CancellationToken cancellationToken)
    {
        var purchases = await _dbContext.GoodsReceipts.AsNoTracking()
            .GroupBy(r => r.SupplierId)
            .Select(g => new { SupplierId = g.Key, Name = g.Max(r => r.SupplierName), Total = g.Sum(r => r.NetAmount), Last = g.Max(r => r.ReceivedDate) })
            .ToListAsync(cancellationToken);

        var returns = await _dbContext.PurchaseReturns.AsNoTracking()
            .GroupBy(r => r.SupplierId)
            .Select(g => new { SupplierId = g.Key, Name = g.Max(r => r.SupplierName), Total = g.Sum(r => r.NetAmount) })
            .ToListAsync(cancellationToken);

        var payments = await _dbContext.SupplierPayments.AsNoTracking()
            .GroupBy(p => p.SupplierId)
            .Select(g => new { SupplierId = g.Key, Name = g.Max(p => p.SupplierName), Total = g.Sum(p => p.Amount) })
            .ToListAsync(cancellationToken);

        var supplierIds = purchases.Select(p => p.SupplierId).Concat(returns.Select(r => r.SupplierId)).Concat(payments.Select(p => p.SupplierId)).Distinct();

        return supplierIds
            .Select(id =>
            {
                var purchase = purchases.FirstOrDefault(p => p.SupplierId == id);
                var ret = returns.FirstOrDefault(r => r.SupplierId == id);
                var payment = payments.FirstOrDefault(p => p.SupplierId == id);
                return new SupplierTotals(
                    id,
                    purchase?.Name ?? ret?.Name ?? payment?.Name ?? string.Empty,
                    purchase?.Total ?? 0m,
                    ret?.Total ?? 0m,
                    payment?.Total ?? 0m,
                    purchase?.Last);
            })
            .OrderBy(t => t.SupplierName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<SupplierEntry>> GetSupplierEntriesAsync(Guid supplierId, CancellationToken cancellationToken)
    {
        var purchases = await _dbContext.GoodsReceipts.AsNoTracking()
            .Where(r => r.SupplierId == supplierId)
            .Select(r => new SupplierEntry(r.ReceivedDate, r.CreatedAt, SupplierEntryType.Purchase, r.Id, r.ReceiptNo, r.SupplierInvoiceNo, r.NetAmount))
            .ToListAsync(cancellationToken);

        var returns = await _dbContext.PurchaseReturns.AsNoTracking()
            .Where(r => r.SupplierId == supplierId)
            .Select(r => new SupplierEntry(r.ReturnDate, r.CreatedAt, SupplierEntryType.Return, r.Id, r.ReturnNo, r.CreditNoteNo, r.NetAmount))
            .ToListAsync(cancellationToken);

        var payments = await _dbContext.SupplierPayments.AsNoTracking()
            .Where(p => p.SupplierId == supplierId)
            .Select(p => new SupplierEntry(p.PaymentDate, p.CreatedAt, SupplierEntryType.Payment, p.Id, p.PaymentNo, p.ReferenceNo, p.Amount))
            .ToListAsync(cancellationToken);

        return purchases.Concat(returns).Concat(payments).OrderBy(e => e.Date).ThenBy(e => e.CreatedAt).ToList();
    }
}

internal class PharmacyPurchaseOrderConfiguration : IEntityTypeConfiguration<PharmacyPurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PharmacyPurchaseOrder> builder)
    {
        builder.Ignore(o => o.EstimatedAmount);
        builder.Ignore(o => o.IsOpen);
        builder.MapDocument("pharmacy_purchase_orders");

        builder.HasIndex(o => o.OrderNo).IsUnique().HasDatabaseName("ux_pharmacy_purchase_orders_order_no");
        builder.HasIndex(o => o.SupplierId).HasDatabaseName("ix_pharmacy_purchase_orders_supplier_id");
        builder.HasIndex(o => o.Status).HasDatabaseName("ix_pharmacy_purchase_orders_status");

        builder.HasOne<PharmacyStore>().WithMany().HasForeignKey(o => o.StoreId)
            .HasConstraintName("fk_pharmacy_purchase_orders_pharmacy_stores_store_id").OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.PurchaseOrderId)
            .HasConstraintName("fk_pharmacy_purchase_order_lines_purchase_order_id").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(o => o.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal class PharmacyPurchaseOrderLineConfiguration : IEntityTypeConfiguration<PharmacyPurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PharmacyPurchaseOrderLine> builder)
    {
        builder.Ignore(l => l.PendingQuantity);
        builder.MapDocument("pharmacy_purchase_order_lines");

        builder.HasIndex(l => l.PurchaseOrderId).HasDatabaseName("ix_pharmacy_purchase_order_lines_purchase_order_id");
        builder.HasIndex(l => l.ProductId).HasDatabaseName("ix_pharmacy_purchase_order_lines_product_id");
    }
}

internal class PharmacyPurchaseReturnConfiguration : IEntityTypeConfiguration<PharmacyPurchaseReturn>
{
    public void Configure(EntityTypeBuilder<PharmacyPurchaseReturn> builder)
    {
        builder.MapDocument("pharmacy_purchase_returns");

        builder.HasIndex(r => r.ReturnNo).IsUnique().HasDatabaseName("ux_pharmacy_purchase_returns_return_no");
        builder.HasIndex(r => r.SupplierId).HasDatabaseName("ix_pharmacy_purchase_returns_supplier_id");
        builder.HasIndex(r => r.ReturnDate).HasDatabaseName("ix_pharmacy_purchase_returns_return_date");

        builder.HasOne<PharmacyStore>().WithMany().HasForeignKey(r => r.StoreId)
            .HasConstraintName("fk_pharmacy_purchase_returns_pharmacy_stores_store_id").OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Lines).WithOne().HasForeignKey(l => l.PurchaseReturnId)
            .HasConstraintName("fk_pharmacy_purchase_return_lines_purchase_return_id").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal class PharmacyPurchaseReturnLineConfiguration : IEntityTypeConfiguration<PharmacyPurchaseReturnLine>
{
    public void Configure(EntityTypeBuilder<PharmacyPurchaseReturnLine> builder)
    {
        builder.Ignore(l => l.LineTotal);
        builder.MapDocument("pharmacy_purchase_return_lines");

        builder.HasIndex(l => l.PurchaseReturnId).HasDatabaseName("ix_pharmacy_purchase_return_lines_purchase_return_id");
    }
}

internal class PharmacySupplierPaymentConfiguration : IEntityTypeConfiguration<PharmacySupplierPayment>
{
    public void Configure(EntityTypeBuilder<PharmacySupplierPayment> builder)
    {
        builder.MapDocument("pharmacy_supplier_payments");

        builder.HasIndex(p => p.PaymentNo).IsUnique().HasDatabaseName("ux_pharmacy_supplier_payments_payment_no");
        builder.HasIndex(p => p.SupplierId).HasDatabaseName("ix_pharmacy_supplier_payments_supplier_id");
    }
}
