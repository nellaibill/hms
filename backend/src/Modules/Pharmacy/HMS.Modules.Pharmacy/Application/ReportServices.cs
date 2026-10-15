using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Products.Application;
using HMS.Modules.Products.Contracts;

namespace HMS.Modules.Pharmacy.Application;

// ---- What the repository hands back: flat rows, already narrowed to the period ----------

/// <summary>One bill, as far as a day's totals need it. Cost is the cost of its lines.</summary>
internal sealed record SaleDayRow(DateTime SaleDate, decimal Gross, decimal Discount, decimal Taxable, decimal Tax, decimal Net, decimal? Cost);

/// <summary>One line of a sale return, with the HSN code and unit cost of the bill line it reverses.</summary>
internal sealed record SaleReturnLineRow(DateTime ReturnDate, Guid ProductId, string ProductName, string? HsnCode, decimal GstRatePercent, decimal Quantity, decimal Taxable, decimal Tax, decimal? UnitCost);

internal sealed record PaymentRow(DateTime PaidAt, SalePaymentMethod Method, decimal Amount);

/// <summary>A product's bill lines in a period, summed.</summary>
internal sealed record ProductSoldRow(Guid ProductId, string ProductName, int Bills, decimal Quantity, decimal Taxable, decimal Tax, decimal? Cost);

/// <summary>Taxable value and tax at one HSN code and rate, within or across the state.</summary>
internal sealed record TaxGroupRow(string? HsnCode, decimal GstRatePercent, bool IsInterState, decimal Taxable, decimal Tax);

/// <summary>Read-only queries behind the reports. Nothing here changes anything.</summary>
internal interface IPharmacyReportRepository
{
    Task<IReadOnlyList<SaleDayRow>> GetSaleDayRowsAsync(DateTime fromUtc, DateTime toUtc, Guid? storeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SalesRegisterRow>> GetSalesRegisterAsync(DateTime fromUtc, DateTime toUtc, Guid? storeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SalesReturnRegisterRow>> GetSaleReturnsAsync(DateTime fromUtc, DateTime toUtc, Guid? storeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SaleReturnLineRow>> GetSaleReturnLinesAsync(DateTime fromUtc, DateTime toUtc, Guid? storeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PaymentRow>> GetPaymentsAsync(DateTime fromUtc, DateTime toUtc, Guid? storeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductSoldRow>> GetProductSalesAsync(DateTime fromUtc, DateTime toUtc, Guid? storeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TaxGroupRow>> GetSaleTaxGroupsAsync(DateTime fromUtc, DateTime toUtc, Guid? storeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PurchaseRegisterRow>> GetPurchasesAsync(DateOnly fromDate, DateOnly toDate, Guid? storeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PurchaseReturnRegisterRow>> GetPurchaseReturnsAsync(DateOnly fromDate, DateOnly toDate, Guid? storeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TaxGroupRow>> GetPurchaseTaxGroupsAsync(DateOnly fromDate, DateOnly toDate, Guid? storeId, CancellationToken cancellationToken);

    /// <summary>Purchase-return lines carry neither HSN code nor the inter-state flag; both are
    /// taken from the goods receipt the batch last came in on.</summary>
    Task<IReadOnlyList<TaxGroupRow>> GetPurchaseReturnTaxGroupsAsync(DateOnly fromDate, DateOnly toDate, Guid? storeId, CancellationToken cancellationToken);

    /// <summary>When each product was last billed.</summary>
    Task<IReadOnlyDictionary<Guid, DateTime>> GetLastSaleDatesAsync(Guid? storeId, CancellationToken cancellationToken);

    /// <summary>Bills still owed at the counter — not those charged to an admission.</summary>
    Task<(int Bills, decimal Amount)> GetCustomerDuesAsync(Guid? storeId, CancellationToken cancellationToken);

    /// <summary>Goods receipts less purchase returns less payments, across every supplier.</summary>
    Task<decimal> GetSupplierPayableAsync(CancellationToken cancellationToken);

    Task<int> CountPendingIndentsAsync(CancellationToken cancellationToken);

    Task<int> CountOpenPurchaseOrdersAsync(Guid? storeId, CancellationToken cancellationToken);
}

/// <summary>
/// The arithmetic of the reports, kept apart from where the rows come from so it can be
/// checked by hand: every method takes rows and gives back the table the screen shows.
/// </summary>
internal static class ReportMath
{
    public static IReadOnlyList<DailySalesRow> BuildDaily(
        DateOnly fromDate,
        DateOnly toDate,
        IReadOnlyList<SaleDayRow> sales,
        IReadOnlyList<SalesReturnRegisterRow> returns,
        IReadOnlyList<SaleReturnLineRow> returnLines,
        IReadOnlyList<PaymentRow> payments,
        bool includeCost)
    {
        var salesByDay = sales.ToLookup(s => PharmacyClock.LocalDate(s.SaleDate));
        var returnsByDay = returns.ToLookup(r => PharmacyClock.LocalDate(r.ReturnDate));
        var returnCostByDay = returnLines.ToLookup(l => PharmacyClock.LocalDate(l.ReturnDate));
        var paymentsByDay = payments.ToLookup(p => PharmacyClock.LocalDate(p.PaidAt));

        var rows = new List<DailySalesRow>();
        for (var day = fromDate; day <= toDate; day = day.AddDays(1))
        {
            var daySales = salesByDay[day].ToList();
            var dayReturns = returnsByDay[day].ToList();

            var taxable = daySales.Sum(s => s.Taxable);
            var salesAmount = daySales.Sum(s => s.Net);
            var returnAmount = dayReturns.Sum(r => r.ReturnAmount);
            var cost = daySales.Sum(s => s.Cost ?? 0m) - returnCostByDay[day].Sum(l => l.Quantity * (l.UnitCost ?? 0m));
            var netTaxable = taxable - dayReturns.Sum(r => r.TaxableAmount);

            rows.Add(new DailySalesRow
            {
                Date = day,
                Bills = daySales.Count,
                GrossAmount = GstMath.Round2(daySales.Sum(s => s.Gross)),
                DiscountAmount = GstMath.Round2(daySales.Sum(s => s.Discount)),
                TaxableAmount = GstMath.Round2(taxable),
                TaxAmount = GstMath.Round2(daySales.Sum(s => s.Tax)),
                SalesAmount = salesAmount,
                Returns = dayReturns.Count,
                ReturnAmount = returnAmount,
                NetSales = salesAmount - returnAmount,
                Collected = paymentsByDay[day].Sum(p => p.Amount) - dayReturns.Sum(r => r.RefundAmount),
                Cost = includeCost ? GstMath.Round2(cost) : null,
                Margin = includeCost ? GstMath.Round2(netTaxable - cost) : null,
            });
        }

        return rows;
    }

    public static SalesFigures Total(IEnumerable<DailySalesRow> days)
    {
        var rows = days.ToList();
        return new SalesFigures
        {
            Bills = rows.Sum(d => d.Bills),
            SalesAmount = rows.Sum(d => d.SalesAmount),
            ReturnAmount = rows.Sum(d => d.ReturnAmount),
            NetSales = rows.Sum(d => d.NetSales),
            Collected = rows.Sum(d => d.Collected),
            Margin = rows.Any(d => d.Margin is not null) ? rows.Sum(d => d.Margin ?? 0m) : null,
        };
    }

    /// <summary>Sales per product net of returns, best seller by value first. A product that
    /// only came back in the period (sold earlier) still shows, with negative figures.</summary>
    public static IReadOnlyList<ProductSalesRow> BuildProductSales(IReadOnlyList<ProductSoldRow> sold, IReadOnlyList<SaleReturnLineRow> returnLines, bool includeCost)
    {
        var returned = returnLines
            .GroupBy(l => l.ProductId)
            .ToDictionary(
                g => g.Key,
                g => (Name: g.First().ProductName, Quantity: g.Sum(l => l.Quantity), Taxable: g.Sum(l => l.Taxable), Tax: g.Sum(l => l.Tax), Cost: g.Sum(l => l.Quantity * (l.UnitCost ?? 0m))));

        var rows = new List<ProductSalesRow>();
        foreach (var product in sold)
        {
            returned.Remove(product.ProductId, out var back);
            rows.Add(Row(product.ProductId, product.ProductName, product.Bills, product.Quantity, back.Quantity, product.Taxable - back.Taxable, product.Tax - back.Tax, (product.Cost ?? 0m) - back.Cost));
        }

        foreach (var (productId, back) in returned)
        {
            rows.Add(Row(productId, back.Name, 0, 0m, back.Quantity, -back.Taxable, -back.Tax, -back.Cost));
        }

        return rows.OrderByDescending(r => r.Amount).ThenBy(r => r.ProductName, StringComparer.OrdinalIgnoreCase).ToList();

        ProductSalesRow Row(Guid productId, string name, int bills, decimal quantitySold, decimal quantityReturned, decimal taxable, decimal tax, decimal cost) => new()
        {
            ProductId = productId,
            ProductName = name,
            Bills = bills,
            QuantitySold = quantitySold,
            QuantityReturned = quantityReturned,
            NetQuantity = quantitySold - quantityReturned,
            TaxableAmount = GstMath.Round2(taxable),
            TaxAmount = GstMath.Round2(tax),
            Amount = GstMath.Round2(taxable + tax),
            Cost = includeCost ? GstMath.Round2(cost) : null,
            Margin = includeCost ? GstMath.Round2(taxable - cost) : null,
            MarginPercent = includeCost && taxable > 0 ? Math.Round((taxable - cost) / taxable * 100m, 1, MidpointRounding.AwayFromZero) : null,
        };
    }

    /// <summary>Money in and money back per day and payment mode. A refund counts on the day
    /// it was handed over, in the mode it was handed over in.</summary>
    public static IReadOnlyList<CollectionRow> BuildCollections(IReadOnlyList<PaymentRow> payments, IReadOnlyList<SalesReturnRegisterRow> returns)
    {
        var received = payments
            .GroupBy(p => (Date: PharmacyClock.LocalDate(p.PaidAt), p.Method))
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        var refunded = returns
            .Where(r => r.RefundAmount > 0)
            .GroupBy(r => (Date: PharmacyClock.LocalDate(r.ReturnDate), Method: r.RefundMethod))
            .ToDictionary(g => g.Key, g => g.Sum(r => r.RefundAmount));

        return received.Keys.Union(refunded.Keys)
            .OrderBy(k => k.Date).ThenBy(k => k.Method)
            .Select(k =>
            {
                var moneyIn = received.GetValueOrDefault(k);
                var moneyBack = refunded.GetValueOrDefault(k);
                return new CollectionRow { Date = k.Date, Method = k.Method, Received = moneyIn, Refunded = moneyBack, Net = moneyIn - moneyBack };
            })
            .ToList();
    }

    /// <summary>Rolls tax groups up by HSN code and rate. Tax within the state is split evenly
    /// into CGST and SGST (any odd paisa to SGST); tax across states is IGST.</summary>
    public static IReadOnlyList<GstRateRow> BuildGstRows(IEnumerable<TaxGroupRow> groups)
        => groups
            .GroupBy(g => (Hsn: string.IsNullOrWhiteSpace(g.HsnCode) ? null : g.HsnCode.Trim(), g.GstRatePercent))
            .Select(g =>
            {
                var within = GstMath.Round2(g.Where(x => !x.IsInterState).Sum(x => x.Tax));
                var across = GstMath.Round2(g.Where(x => x.IsInterState).Sum(x => x.Tax));
                var cgst = Math.Round(within / 2m, 2, MidpointRounding.ToZero);

                return new GstRateRow
                {
                    HsnCode = g.Key.Hsn,
                    GstRatePercent = g.Key.GstRatePercent,
                    TaxableAmount = GstMath.Round2(g.Sum(x => x.Taxable)),
                    Cgst = cgst,
                    Sgst = within - cgst,
                    Igst = across,
                    TaxAmount = within + across,
                };
            })
            .Where(r => r.TaxableAmount != 0 || r.TaxAmount != 0)
            .OrderBy(r => r.GstRatePercent).ThenBy(r => r.HsnCode, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static StockValuationResponse BuildValuation(
        IReadOnlyList<StoreStockLine> stock,
        IReadOnlyDictionary<Guid, ProductResponse> products,
        IReadOnlyDictionary<Guid, ProductBatchResponse> batches,
        DateOnly today,
        bool includeCost)
    {
        var rows = stock
            .Where(s => products.ContainsKey(s.ProductId) && batches.ContainsKey(s.ProductBatchId))
            .GroupBy(s => s.ProductId)
            .Select(g =>
            {
                var product = products[g.Key];
                var lines = g.Select(s => (s.QuantityOnHand, Batch: batches[s.ProductBatchId])).ToList();
                var costed = lines.Where(l => PharmacyPricing.UnitCost(product, l.Batch) is not null).ToList();
                var expired = lines.Where(l => l.Batch.ExpiryDate < today).ToList();

                return new StockValuationRow
                {
                    ProductId = product.Id,
                    ProductName = product.ProductName,
                    GenericName = product.GenericName,
                    RackLocation = product.RackLocation,
                    Batches = lines.Select(l => l.Batch.Id).Distinct().Count(),
                    Quantity = lines.Sum(l => l.QuantityOnHand),
                    CostValue = includeCost && costed.Count > 0 ? GstMath.Round2(costed.Sum(l => l.QuantityOnHand * PharmacyPricing.UnitCost(product, l.Batch)!.Value)) : null,
                    MrpValue = GstMath.Round2(lines.Sum(l => l.QuantityOnHand * PharmacyPricing.UnitMrp(product, l.Batch))),
                    ExpiredQuantity = expired.Sum(l => l.QuantityOnHand),
                    ExpiredCostValue = includeCost && expired.Count > 0 ? GstMath.Round2(expired.Sum(l => l.QuantityOnHand * (PharmacyPricing.UnitCost(product, l.Batch) ?? 0m))) : null,
                };
            })
            .OrderBy(r => r.ProductName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new StockValuationResponse
        {
            Rows = rows,
            TotalQuantity = rows.Sum(r => r.Quantity),
            TotalCostValue = includeCost ? rows.Sum(r => r.CostValue ?? 0m) : null,
            TotalMrpValue = rows.Sum(r => r.MrpValue),
            ExpiredCostValue = includeCost ? rows.Sum(r => r.ExpiredCostValue ?? 0m) : null,
        };
    }

    /// <summary>Products with stock on the shelf that have not been billed for
    /// <paramref name="idleDays"/> days. A product never billed is measured from the day its
    /// oldest batch still in stock was recorded, so last week's delivery is not called dead.</summary>
    public static IReadOnlyList<NonMovingStockRow> BuildNonMoving(
        IReadOnlyList<StoreStockLine> stock,
        IReadOnlyDictionary<Guid, ProductResponse> products,
        IReadOnlyDictionary<Guid, ProductBatchResponse> batches,
        IReadOnlyDictionary<Guid, DateTime> lastSold,
        DateOnly today,
        int idleDays,
        bool includeCost)
        => stock
            .Where(s => products.ContainsKey(s.ProductId) && batches.ContainsKey(s.ProductBatchId))
            .GroupBy(s => s.ProductId)
            .Select(g =>
            {
                var product = products[g.Key];
                var lines = g.Select(s => (s.QuantityOnHand, Batch: batches[s.ProductBatchId])).ToList();
                DateOnly? lastSoldOn = lastSold.TryGetValue(product.Id, out var soldAt) ? PharmacyClock.LocalDate(soldAt) : null;
                var stockedOn = PharmacyClock.LocalDate(lines.Min(l => l.Batch.CreatedAt));
                var unexpired = lines.Where(l => l.Batch.ExpiryDate >= today).ToList();

                return new NonMovingStockRow
                {
                    ProductId = product.Id,
                    ProductName = product.ProductName,
                    RackLocation = product.RackLocation,
                    Quantity = lines.Sum(l => l.QuantityOnHand),
                    CostValue = includeCost ? GstMath.Round2(lines.Sum(l => l.QuantityOnHand * (PharmacyPricing.UnitCost(product, l.Batch) ?? 0m))) : null,
                    LastSoldOn = lastSoldOn,
                    StockedOn = stockedOn,
                    IdleDays = today.DayNumber - (lastSoldOn ?? stockedOn).DayNumber,
                    NearestExpiry = unexpired.Count > 0 ? unexpired.Min(l => l.Batch.ExpiryDate) : null,
                };
            })
            .Where(r => r.IdleDays >= idleDays)
            .OrderByDescending(r => r.IdleDays).ThenBy(r => r.ProductName, StringComparer.OrdinalIgnoreCase)
            .ToList();
}

/// <summary>Pharmacy reports and the dashboard. Read-only; cost, margin and stock value are
/// left out unless <c>includeCost</c> is set (pharmacy.view-cost).</summary>
public interface IPharmacyReportService
{
    Task<PharmacyDashboardResponse> GetDashboardAsync(Guid? storeId, bool includeCost, CancellationToken cancellationToken);

    Task<IReadOnlyList<DailySalesRow>> GetDailySalesAsync(DateOnly fromDate, DateOnly toDate, Guid? storeId, bool includeCost, CancellationToken cancellationToken);

    Task<SalesRegisterResponse> GetSalesRegisterAsync(DateOnly fromDate, DateOnly toDate, Guid? storeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductSalesRow>> GetProductSalesAsync(DateOnly fromDate, DateOnly toDate, Guid? storeId, bool includeCost, CancellationToken cancellationToken);

    Task<IReadOnlyList<CollectionRow>> GetCollectionsAsync(DateOnly fromDate, DateOnly toDate, Guid? storeId, CancellationToken cancellationToken);

    Task<GstSummaryResponse> GetGstSummaryAsync(DateOnly fromDate, DateOnly toDate, Guid? storeId, bool includeCost, CancellationToken cancellationToken);

    Task<PurchaseRegisterResponse> GetPurchaseRegisterAsync(DateOnly fromDate, DateOnly toDate, Guid? storeId, CancellationToken cancellationToken);

    Task<StockValuationResponse> GetStockValuationAsync(Guid? storeId, bool includeCost, CancellationToken cancellationToken);

    Task<IReadOnlyList<NonMovingStockRow>> GetNonMovingStockAsync(Guid? storeId, int idleDays, bool includeCost, CancellationToken cancellationToken);
}

internal class PharmacyReportService : IPharmacyReportService
{
    private const int TrendDays = 14;
    private const int TopProductDays = 30;
    private const int TopProductCount = 8;

    private readonly IPharmacyReportRepository _repository;
    private readonly IPharmacyStockControlRepository _stockRepository;
    private readonly IPharmacySettingsRepository _settingsRepository;
    private readonly IStockBalanceService _stockBalanceService;
    private readonly IProductService _productService;
    private readonly IProductBatchService _productBatchService;

    public PharmacyReportService(
        IPharmacyReportRepository repository,
        IPharmacyStockControlRepository stockRepository,
        IPharmacySettingsRepository settingsRepository,
        IStockBalanceService stockBalanceService,
        IProductService productService,
        IProductBatchService productBatchService)
    {
        _repository = repository;
        _stockRepository = stockRepository;
        _settingsRepository = settingsRepository;
        _stockBalanceService = stockBalanceService;
        _productService = productService;
        _productBatchService = productBatchService;
    }

    public async Task<PharmacyDashboardResponse> GetDashboardAsync(Guid? storeId, bool includeCost, CancellationToken cancellationToken)
    {
        var today = PharmacyClock.Today();
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var trendStart = today.AddDays(-(TrendDays - 1));
        var fromDate = monthStart < trendStart ? monthStart : trendStart;

        var period = await LoadSalesAsync(fromDate, today, storeId, cancellationToken);
        var daily = ReportMath.BuildDaily(fromDate, today, period.Sales, period.Returns, period.ReturnLines, period.Payments, includeCost);

        var todayCollections = ReportMath.BuildCollections(period.Payments, period.Returns)
            .Where(c => c.Date == today && c.Net != 0)
            .Select(c => new PaymentModeTotal { Method = c.Method, Amount = c.Net })
            .ToList();

        var topProducts = (await GetProductSalesAsync(today.AddDays(-(TopProductDays - 1)), today, storeId, includeCost, cancellationToken))
            .Where(p => p.Amount > 0)
            .Take(TopProductCount)
            .ToList();

        var (dueBills, dueAmount) = await _repository.GetCustomerDuesAsync(storeId, cancellationToken);

        return new PharmacyDashboardResponse
        {
            Date = today,
            Today = ReportMath.Total(daily.Where(d => d.Date == today)),
            Yesterday = ReportMath.Total(daily.Where(d => d.Date == today.AddDays(-1))),
            MonthToDate = ReportMath.Total(daily.Where(d => d.Date >= monthStart)),
            Trend = daily.Where(d => d.Date >= trendStart).ToList(),
            TodayCollections = todayCollections,
            Stock = await GetStockAlertsAsync(storeId, today, includeCost, cancellationToken),
            Pending = new PendingWorkFigures
            {
                PendingIndents = await _repository.CountPendingIndentsAsync(cancellationToken),
                OpenPurchaseOrders = await _repository.CountOpenPurchaseOrdersAsync(storeId, cancellationToken),
                BillsWithDues = dueBills,
                CustomerDues = dueAmount,
                SupplierPayable = includeCost ? await _repository.GetSupplierPayableAsync(cancellationToken) : null,
            },
            TopProducts = topProducts,
        };
    }

    public async Task<IReadOnlyList<DailySalesRow>> GetDailySalesAsync(DateOnly fromDate, DateOnly toDate, Guid? storeId, bool includeCost, CancellationToken cancellationToken)
    {
        var period = await LoadSalesAsync(fromDate, toDate, storeId, cancellationToken);
        return ReportMath.BuildDaily(fromDate, toDate, period.Sales, period.Returns, period.ReturnLines, period.Payments, includeCost);
    }

    public async Task<SalesRegisterResponse> GetSalesRegisterAsync(DateOnly fromDate, DateOnly toDate, Guid? storeId, CancellationToken cancellationToken)
    {
        var (fromUtc, toUtc) = Range(fromDate, toDate);
        return new SalesRegisterResponse
        {
            Bills = await _repository.GetSalesRegisterAsync(fromUtc, toUtc, storeId, cancellationToken),
            Returns = await _repository.GetSaleReturnsAsync(fromUtc, toUtc, storeId, cancellationToken),
        };
    }

    public async Task<IReadOnlyList<ProductSalesRow>> GetProductSalesAsync(DateOnly fromDate, DateOnly toDate, Guid? storeId, bool includeCost, CancellationToken cancellationToken)
    {
        var (fromUtc, toUtc) = Range(fromDate, toDate);
        var sold = await _repository.GetProductSalesAsync(fromUtc, toUtc, storeId, cancellationToken);
        var returnLines = await _repository.GetSaleReturnLinesAsync(fromUtc, toUtc, storeId, cancellationToken);
        return ReportMath.BuildProductSales(sold, returnLines, includeCost);
    }

    public async Task<IReadOnlyList<CollectionRow>> GetCollectionsAsync(DateOnly fromDate, DateOnly toDate, Guid? storeId, CancellationToken cancellationToken)
    {
        var (fromUtc, toUtc) = Range(fromDate, toDate);
        var payments = await _repository.GetPaymentsAsync(fromUtc, toUtc, storeId, cancellationToken);
        var returns = await _repository.GetSaleReturnsAsync(fromUtc, toUtc, storeId, cancellationToken);
        return ReportMath.BuildCollections(payments, returns);
    }

    public async Task<GstSummaryResponse> GetGstSummaryAsync(DateOnly fromDate, DateOnly toDate, Guid? storeId, bool includeCost, CancellationToken cancellationToken)
    {
        var (fromUtc, toUtc) = Range(fromDate, toDate);

        var sales = ReportMath.BuildGstRows(await _repository.GetSaleTaxGroupsAsync(fromUtc, toUtc, storeId, cancellationToken));
        var saleReturns = ReportMath.BuildGstRows(
            (await _repository.GetSaleReturnLinesAsync(fromUtc, toUtc, storeId, cancellationToken))
            .Select(l => new TaxGroupRow(l.HsnCode, l.GstRatePercent, IsInterState: false, l.Taxable, l.Tax)));
        IReadOnlyList<GstRateRow> purchases = includeCost ? ReportMath.BuildGstRows(await _repository.GetPurchaseTaxGroupsAsync(fromDate, toDate, storeId, cancellationToken)) : [];
        IReadOnlyList<GstRateRow> purchaseReturns = includeCost ? ReportMath.BuildGstRows(await _repository.GetPurchaseReturnTaxGroupsAsync(fromDate, toDate, storeId, cancellationToken)) : [];

        return new GstSummaryResponse
        {
            Sales = sales,
            SaleReturns = saleReturns,
            Purchases = purchases,
            PurchaseReturns = purchaseReturns,
            OutputTax = sales.Sum(r => r.TaxAmount) - saleReturns.Sum(r => r.TaxAmount),
            InputTax = includeCost ? purchases.Sum(r => r.TaxAmount) - purchaseReturns.Sum(r => r.TaxAmount) : null,
        };
    }

    public async Task<PurchaseRegisterResponse> GetPurchaseRegisterAsync(DateOnly fromDate, DateOnly toDate, Guid? storeId, CancellationToken cancellationToken)
        => new()
        {
            Purchases = await _repository.GetPurchasesAsync(fromDate, toDate, storeId, cancellationToken),
            Returns = await _repository.GetPurchaseReturnsAsync(fromDate, toDate, storeId, cancellationToken),
        };

    public async Task<StockValuationResponse> GetStockValuationAsync(Guid? storeId, bool includeCost, CancellationToken cancellationToken)
    {
        var (stock, products, batches) = await LoadStockAsync(storeId, cancellationToken);
        return ReportMath.BuildValuation(stock, products, batches, PharmacyClock.Today(), includeCost);
    }

    public async Task<IReadOnlyList<NonMovingStockRow>> GetNonMovingStockAsync(Guid? storeId, int idleDays, bool includeCost, CancellationToken cancellationToken)
    {
        var (stock, products, batches) = await LoadStockAsync(storeId, cancellationToken);
        var lastSold = await _repository.GetLastSaleDatesAsync(storeId, cancellationToken);
        return ReportMath.BuildNonMoving(stock, products, batches, lastSold, PharmacyClock.Today(), Math.Clamp(idleDays, 1, 3650), includeCost);
    }

    private async Task<StockAlertFigures> GetStockAlertsAsync(Guid? storeId, DateOnly today, bool includeCost, CancellationToken cancellationToken)
    {
        var (stock, products, batches) = await LoadStockAsync(storeId, cancellationToken);
        var alertDays = await _settingsRepository.ExpiryAlertDaysAsync(cancellationToken);
        var cutoff = today.AddDays(alertDays);

        var known = stock.Where(s => batches.ContainsKey(s.ProductBatchId)).ToList();
        var valuation = ReportMath.BuildValuation(stock, products, batches, today, includeCost);
        var reorder = await _stockBalanceService.GetSummariesAsync(StockSummaryFilter.NeedsReorder, storeId, cancellationToken);

        return new StockAlertFigures
        {
            ProductsInStock = valuation.Rows.Count,
            LowStock = reorder.Count(r => r.Status == StockStatus.LowStock),
            OutOfStock = reorder.Count(r => r.Status == StockStatus.OutOfStock),
            ExpiredBatches = known.Count(s => batches[s.ProductBatchId].ExpiryDate < today),
            ExpiringSoonBatches = known.Count(s => batches[s.ProductBatchId].ExpiryDate >= today && batches[s.ProductBatchId].ExpiryDate <= cutoff),
            ExpiryAlertDays = alertDays,
            StockValue = valuation.TotalCostValue,
            ExpiredValue = valuation.ExpiredCostValue,
        };
    }

    private async Task<(IReadOnlyList<StoreStockLine> Stock, IReadOnlyDictionary<Guid, ProductResponse> Products, IReadOnlyDictionary<Guid, ProductBatchResponse> Batches)> LoadStockAsync(Guid? storeId, CancellationToken cancellationToken)
    {
        var stock = await _stockRepository.GetStockLinesAsync(storeId, cancellationToken);
        var products = await _productService.GetLookupAsync(stock.Select(s => s.ProductId), cancellationToken);
        var batches = await _productBatchService.GetLookupAsync(stock.Select(s => s.ProductBatchId), cancellationToken);
        return (stock, products, batches);
    }

    private async Task<(IReadOnlyList<SaleDayRow> Sales, IReadOnlyList<SalesReturnRegisterRow> Returns, IReadOnlyList<SaleReturnLineRow> ReturnLines, IReadOnlyList<PaymentRow> Payments)> LoadSalesAsync(
        DateOnly fromDate, DateOnly toDate, Guid? storeId, CancellationToken cancellationToken)
    {
        var (fromUtc, toUtc) = Range(fromDate, toDate);
        return (
            await _repository.GetSaleDayRowsAsync(fromUtc, toUtc, storeId, cancellationToken),
            await _repository.GetSaleReturnsAsync(fromUtc, toUtc, storeId, cancellationToken),
            await _repository.GetSaleReturnLinesAsync(fromUtc, toUtc, storeId, cancellationToken),
            await _repository.GetPaymentsAsync(fromUtc, toUtc, storeId, cancellationToken));
    }

    /// <summary>The UTC instants a run of hospital-local days starts and ends at (end exclusive).</summary>
    private static (DateTime FromUtc, DateTime ToUtc) Range(DateOnly fromDate, DateOnly toDate)
        => (PharmacyClock.StartOfDayUtc(fromDate), PharmacyClock.StartOfDayUtc(toDate.AddDays(1)));
}
