using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Pharmacy.Infrastructure.Repositories;

internal class PharmacyReportRepository : IPharmacyReportRepository
{
    private readonly PharmacyDbContext _dbContext;

    public PharmacyReportRepository(PharmacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // ---- Sales

    public async Task<IReadOnlyList<SaleDayRow>> GetSaleDayRowsAsync(DateTime fromUtc, DateTime toUtc, Guid? storeId, CancellationToken cancellationToken)
    {
        var rows = await Sales(fromUtc, toUtc, storeId)
            .Select(s => new { s.SaleDate, s.GrossAmount, s.DiscountAmount, s.TaxableAmount, s.TaxAmount, s.NetAmount, Cost = s.Lines.Sum(l => l.Quantity * l.UnitCost) })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new SaleDayRow(r.SaleDate, r.GrossAmount, r.DiscountAmount, r.TaxableAmount, r.TaxAmount, r.NetAmount, r.Cost)).ToList();
    }

    public async Task<IReadOnlyList<SalesRegisterRow>> GetSalesRegisterAsync(DateTime fromUtc, DateTime toUtc, Guid? storeId, CancellationToken cancellationToken)
    {
        var sales = await Sales(fromUtc, toUtc, storeId).OrderBy(s => s.SaleDate).ToListAsync(cancellationToken);

        return sales
            .Select(s => new SalesRegisterRow
            {
                SaleId = s.Id,
                BillNo = s.BillNo,
                SaleDate = s.SaleDate,
                CustomerName = s.CustomerName,
                PatientUhid = s.PatientUhid,
                Kind = s.AdmissionId is not null ? "IP" : s.PatientId is not null ? "OP" : "Walk-in",
                DoctorName = s.DoctorName,
                GrossAmount = s.GrossAmount,
                DiscountAmount = s.DiscountAmount,
                TaxableAmount = s.TaxableAmount,
                TaxAmount = s.TaxAmount,
                RoundOff = s.RoundOff,
                NetAmount = s.NetAmount,
                PaidAmount = s.PaidAmount - s.RefundedAmount,
                ReturnedAmount = s.ReturnedAmount,
                BalanceDue = s.BalanceDue,
                ChargedToAdmission = s.ChargedToAdmission,
            })
            .ToList();
    }

    public async Task<IReadOnlyList<SalesReturnRegisterRow>> GetSaleReturnsAsync(DateTime fromUtc, DateTime toUtc, Guid? storeId, CancellationToken cancellationToken)
        => await SaleReturns(fromUtc, toUtc, storeId)
            .OrderBy(r => r.ReturnDate)
            .Select(r => new SalesReturnRegisterRow
            {
                ReturnId = r.Id,
                ReturnNo = r.ReturnNo,
                SaleId = r.SaleId,
                BillNo = r.BillNo,
                ReturnDate = r.ReturnDate,
                CustomerName = r.CustomerName,
                TaxableAmount = r.TaxableAmount,
                TaxAmount = r.TaxAmount,
                ReturnAmount = r.ReturnAmount,
                RefundAmount = r.RefundAmount,
                RefundMethod = r.RefundMethod,
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SaleReturnLineRow>> GetSaleReturnLinesAsync(DateTime fromUtc, DateTime toUtc, Guid? storeId, CancellationToken cancellationToken)
    {
        var rows = await (
            from saleReturn in SaleReturns(fromUtc, toUtc, storeId)
            join line in _dbContext.SaleReturnLines.AsNoTracking() on saleReturn.Id equals line.SaleReturnId
            join saleLine in _dbContext.SaleLines.AsNoTracking() on line.SaleLineId equals saleLine.Id
            select new { saleReturn.ReturnDate, line.ProductId, line.ProductName, saleLine.HsnCode, line.GstRatePercent, line.Quantity, line.TaxableAmount, line.TaxAmount, saleLine.UnitCost })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new SaleReturnLineRow(r.ReturnDate, r.ProductId, r.ProductName, r.HsnCode, r.GstRatePercent, r.Quantity, r.TaxableAmount, r.TaxAmount, r.UnitCost)).ToList();
    }

    public async Task<IReadOnlyList<PaymentRow>> GetPaymentsAsync(DateTime fromUtc, DateTime toUtc, Guid? storeId, CancellationToken cancellationToken)
    {
        var payments = _dbContext.SalePayments.AsNoTracking().Where(p => p.PaidAt >= fromUtc && p.PaidAt < toUtc);
        if (storeId.HasValue)
        {
            var storeSales = _dbContext.Sales.AsNoTracking().Where(s => s.StoreId == storeId.Value).Select(s => s.Id);
            payments = payments.Where(p => storeSales.Contains(p.SaleId));
        }

        var rows = await payments.Select(p => new { p.PaidAt, p.Method, p.Amount }).ToListAsync(cancellationToken);
        return rows.Select(r => new PaymentRow(r.PaidAt, r.Method, r.Amount)).ToList();
    }

    public async Task<IReadOnlyList<ProductSoldRow>> GetProductSalesAsync(DateTime fromUtc, DateTime toUtc, Guid? storeId, CancellationToken cancellationToken)
    {
        var rows = await (
            from sale in Sales(fromUtc, toUtc, storeId)
            join line in _dbContext.SaleLines.AsNoTracking() on sale.Id equals line.SaleId
            group line by line.ProductId into product
            select new
            {
                ProductId = product.Key,
                ProductName = product.Max(l => l.ProductName),
                Bills = product.Select(l => l.SaleId).Distinct().Count(),
                Quantity = product.Sum(l => l.Quantity),
                Taxable = product.Sum(l => l.TaxableAmount),
                Tax = product.Sum(l => l.TaxAmount),
                Cost = product.Sum(l => l.Quantity * l.UnitCost),
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new ProductSoldRow(r.ProductId, r.ProductName!, r.Bills, r.Quantity, r.Taxable, r.Tax, r.Cost)).ToList();
    }

    public async Task<IReadOnlyList<TaxGroupRow>> GetSaleTaxGroupsAsync(DateTime fromUtc, DateTime toUtc, Guid? storeId, CancellationToken cancellationToken)
    {
        var rows = await (
            from sale in Sales(fromUtc, toUtc, storeId)
            join line in _dbContext.SaleLines.AsNoTracking() on sale.Id equals line.SaleId
            group line by new { line.HsnCode, line.GstRatePercent } into rate
            select new { rate.Key.HsnCode, rate.Key.GstRatePercent, Taxable = rate.Sum(l => l.TaxableAmount), Tax = rate.Sum(l => l.TaxAmount) })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new TaxGroupRow(r.HsnCode, r.GstRatePercent, IsInterState: false, r.Taxable, r.Tax)).ToList();
    }

    // ---- Purchases

    public async Task<IReadOnlyList<PurchaseRegisterRow>> GetPurchasesAsync(DateOnly fromDate, DateOnly toDate, Guid? storeId, CancellationToken cancellationToken)
        => await Receipts(fromDate, toDate, storeId)
            .OrderBy(r => r.ReceivedDate).ThenBy(r => r.CreatedAt)
            .Select(r => new PurchaseRegisterRow
            {
                ReceiptId = r.Id,
                ReceiptNo = r.ReceiptNo,
                ReceivedDate = r.ReceivedDate,
                SupplierName = r.SupplierName,
                SupplierInvoiceNo = r.SupplierInvoiceNo,
                SupplierInvoiceDate = r.SupplierInvoiceDate,
                IsInterState = r.IsInterState,
                GrossAmount = r.GrossAmount,
                DiscountAmount = r.DiscountAmount,
                TaxableAmount = r.TaxableAmount,
                TaxAmount = r.TaxAmount,
                OtherCharges = r.OtherCharges,
                RoundOff = r.RoundOff,
                NetAmount = r.NetAmount,
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PurchaseReturnRegisterRow>> GetPurchaseReturnsAsync(DateOnly fromDate, DateOnly toDate, Guid? storeId, CancellationToken cancellationToken)
        => await PurchaseReturns(fromDate, toDate, storeId)
            .OrderBy(r => r.ReturnDate).ThenBy(r => r.CreatedAt)
            .Select(r => new PurchaseReturnRegisterRow
            {
                ReturnId = r.Id,
                ReturnNo = r.ReturnNo,
                ReturnDate = r.ReturnDate,
                SupplierName = r.SupplierName,
                Reason = r.Reason,
                CreditNoteNo = r.CreditNoteNo,
                TaxableAmount = r.TaxableAmount,
                TaxAmount = r.TaxAmount,
                NetAmount = r.NetAmount,
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TaxGroupRow>> GetPurchaseTaxGroupsAsync(DateOnly fromDate, DateOnly toDate, Guid? storeId, CancellationToken cancellationToken)
    {
        var rows = await (
            from receipt in Receipts(fromDate, toDate, storeId)
            join line in _dbContext.GoodsReceiptLines.AsNoTracking() on receipt.Id equals line.GoodsReceiptId
            group line by new { line.HsnCode, line.GstRatePercent, receipt.IsInterState } into rate
            select new { rate.Key.HsnCode, rate.Key.GstRatePercent, rate.Key.IsInterState, Taxable = rate.Sum(l => l.TaxableAmount), Tax = rate.Sum(l => l.TaxAmount) })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new TaxGroupRow(r.HsnCode, r.GstRatePercent, r.IsInterState, r.Taxable, r.Tax)).ToList();
    }

    public async Task<IReadOnlyList<TaxGroupRow>> GetPurchaseReturnTaxGroupsAsync(DateOnly fromDate, DateOnly toDate, Guid? storeId, CancellationToken cancellationToken)
    {
        var lines = await (
            from purchaseReturn in PurchaseReturns(fromDate, toDate, storeId)
            join line in _dbContext.PurchaseReturnLines.AsNoTracking() on purchaseReturn.Id equals line.PurchaseReturnId
            select new { line.ProductBatchId, line.GstRatePercent, line.TaxableAmount, line.TaxAmount })
            .ToListAsync(cancellationToken);

        if (lines.Count == 0)
        {
            return [];
        }

        // The goods receipt each batch last came in on says what HSN code it was bought under
        // and whether that supplier bills IGST.
        var batchIds = lines.Select(l => l.ProductBatchId).Distinct().ToList();
        var receiptLines = await (
            from line in _dbContext.GoodsReceiptLines.AsNoTracking()
            join receipt in _dbContext.GoodsReceipts.AsNoTracking() on line.GoodsReceiptId equals receipt.Id
            where batchIds.Contains(line.ProductBatchId)
            orderby receipt.ReceivedDate descending, receipt.CreatedAt descending
            select new { line.ProductBatchId, line.HsnCode, receipt.IsInterState })
            .ToListAsync(cancellationToken);

        var boughtAs = new Dictionary<Guid, (string? HsnCode, bool IsInterState)>();
        foreach (var receiptLine in receiptLines)
        {
            boughtAs.TryAdd(receiptLine.ProductBatchId, (receiptLine.HsnCode, receiptLine.IsInterState));
        }

        return lines
            .Select(l =>
            {
                var origin = boughtAs.GetValueOrDefault(l.ProductBatchId);
                return new TaxGroupRow(origin.HsnCode, l.GstRatePercent, origin.IsInterState, l.TaxableAmount, l.TaxAmount);
            })
            .ToList();
    }

    // ---- Stock and pending work

    public async Task<IReadOnlyDictionary<Guid, DateTime>> GetLastSaleDatesAsync(Guid? storeId, CancellationToken cancellationToken)
    {
        var sales = _dbContext.Sales.AsNoTracking();
        if (storeId.HasValue)
        {
            sales = sales.Where(s => s.StoreId == storeId.Value);
        }

        var rows = await (
            from sale in sales
            join line in _dbContext.SaleLines.AsNoTracking() on sale.Id equals line.SaleId
            group sale by line.ProductId into product
            select new { ProductId = product.Key, LastSoldAt = product.Max(s => s.SaleDate) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.ProductId, r => r.LastSoldAt);
    }

    public async Task<(int Bills, decimal Amount)> GetCustomerDuesAsync(Guid? storeId, CancellationToken cancellationToken)
    {
        var owing = _dbContext.Sales.AsNoTracking()
            .Where(s => !s.ChargedToAdmission && s.NetAmount - s.ReturnedAmount - (s.PaidAmount - s.RefundedAmount) > 0);
        if (storeId.HasValue)
        {
            owing = owing.Where(s => s.StoreId == storeId.Value);
        }

        var bills = await owing.CountAsync(cancellationToken);
        var amount = bills == 0 ? 0m : await owing.SumAsync(s => s.NetAmount - s.ReturnedAmount - (s.PaidAmount - s.RefundedAmount), cancellationToken);
        return (bills, amount);
    }

    public async Task<decimal> GetSupplierPayableAsync(CancellationToken cancellationToken)
    {
        var purchased = await _dbContext.GoodsReceipts.AsNoTracking().SumAsync(r => r.NetAmount, cancellationToken);
        var returned = await _dbContext.PurchaseReturns.AsNoTracking().SumAsync(r => r.NetAmount, cancellationToken);
        var paid = await _dbContext.SupplierPayments.AsNoTracking().SumAsync(p => p.Amount, cancellationToken);
        return purchased - returned - paid;
    }

    public Task<int> CountPendingIndentsAsync(CancellationToken cancellationToken)
        => _dbContext.Indents.AsNoTracking().CountAsync(i => i.Status == IndentStatus.Pending || i.Status == IndentStatus.PartlyIssued, cancellationToken);

    public Task<int> CountOpenPurchaseOrdersAsync(Guid? storeId, CancellationToken cancellationToken)
        => _dbContext.PurchaseOrders.AsNoTracking()
            .Where(o => o.Status == PurchaseOrderStatus.Ordered || o.Status == PurchaseOrderStatus.PartlyReceived)
            .Where(o => storeId == null || o.StoreId == storeId)
            .CountAsync(cancellationToken);

    // ---- Period filters

    private IQueryable<PharmacySale> Sales(DateTime fromUtc, DateTime toUtc, Guid? storeId)
    {
        var sales = _dbContext.Sales.AsNoTracking().Where(s => s.SaleDate >= fromUtc && s.SaleDate < toUtc);
        return storeId.HasValue ? sales.Where(s => s.StoreId == storeId.Value) : sales;
    }

    private IQueryable<PharmacySaleReturn> SaleReturns(DateTime fromUtc, DateTime toUtc, Guid? storeId)
    {
        var returns = _dbContext.SaleReturns.AsNoTracking().Where(r => r.ReturnDate >= fromUtc && r.ReturnDate < toUtc);
        return storeId.HasValue ? returns.Where(r => r.StoreId == storeId.Value) : returns;
    }

    private IQueryable<PharmacyGoodsReceipt> Receipts(DateOnly fromDate, DateOnly toDate, Guid? storeId)
    {
        var receipts = _dbContext.GoodsReceipts.AsNoTracking().Where(r => r.ReceivedDate >= fromDate && r.ReceivedDate <= toDate);
        return storeId.HasValue ? receipts.Where(r => r.StoreId == storeId.Value) : receipts;
    }

    private IQueryable<PharmacyPurchaseReturn> PurchaseReturns(DateOnly fromDate, DateOnly toDate, Guid? storeId)
    {
        var returns = _dbContext.PurchaseReturns.AsNoTracking().Where(r => r.ReturnDate >= fromDate && r.ReturnDate <= toDate);
        return storeId.HasValue ? returns.Where(r => r.StoreId == storeId.Value) : returns;
    }
}
