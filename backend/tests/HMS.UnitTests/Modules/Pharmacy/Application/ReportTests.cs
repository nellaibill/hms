using FluentAssertions;
using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Modules.Products.Application;
using HMS.Modules.Products.Contracts;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Pharmacy.Application;

public class ReportMathTests
{
    private static readonly DateOnly Monday = new(2026, 10, 12);
    private static readonly DateOnly Tuesday = new(2026, 10, 13);

    /// <summary>A UTC instant at the given hospital-local time on a day.</summary>
    private static DateTime At(DateOnly day, int hour, int minute = 0)
        => PharmacyClock.StartOfDayUtc(day).AddHours(hour).AddMinutes(minute);

    private static SalesReturnRegisterRow Return(DateTime at, decimal taxable, decimal tax, decimal returnAmount, decimal refund, SalePaymentMethod method = SalePaymentMethod.Cash)
        => new() { ReturnId = Guid.NewGuid(), ReturnNo = "SR-1", BillNo = "PH-1", ReturnDate = at, TaxableAmount = taxable, TaxAmount = tax, ReturnAmount = returnAmount, RefundAmount = refund, RefundMethod = method };

    // ---- Daily sales

    [Fact]
    public void Daily_puts_each_bill_on_its_hospital_local_day()
    {
        // 00:30 on Tuesday in India is still Monday in UTC: it must count as Tuesday's.
        var sales = new[]
        {
            new SaleDayRow(At(Monday, 23, 50), 100m, 0m, 89.29m, 10.71m, 100m, 60m),
            new SaleDayRow(At(Tuesday, 0, 30), 200m, 0m, 178.57m, 21.43m, 200m, 120m),
        };

        var rows = ReportMath.BuildDaily(Monday, Tuesday, sales, [], [], [], includeCost: true);

        rows.Should().HaveCount(2);
        rows[0].Should().BeEquivalentTo(new { Date = Monday, Bills = 1, SalesAmount = 100m, NetSales = 100m });
        rows[1].Should().BeEquivalentTo(new { Date = Tuesday, Bills = 1, SalesAmount = 200m, NetSales = 200m });
    }

    [Fact]
    public void Daily_lists_a_day_with_no_business_as_zeros()
    {
        var rows = ReportMath.BuildDaily(Monday, Tuesday, [new SaleDayRow(At(Tuesday, 10), 50m, 0m, 44.64m, 5.36m, 50m, 30m)], [], [], [], includeCost: false);

        rows[0].Should().BeEquivalentTo(new { Date = Monday, Bills = 0, SalesAmount = 0m, Collected = 0m });
    }

    [Fact]
    public void Daily_takes_a_return_off_the_day_it_came_back_and_off_that_days_margin()
    {
        // Sold Monday: taxable 100 on cost 60. Half comes back Tuesday: taxable 50, cost 30.
        var sales = new[] { new SaleDayRow(At(Monday, 11), 112m, 0m, 100m, 12m, 112m, 60m) };
        var returns = new[] { Return(At(Tuesday, 9), taxable: 50m, tax: 6m, returnAmount: 56m, refund: 56m) };
        var returnLines = new[] { new SaleReturnLineRow(At(Tuesday, 9), Guid.NewGuid(), "Dolo 650", "3004", 12m, 5m, 50m, 6m, UnitCost: 6m) };
        var payments = new[] { new PaymentRow(At(Monday, 11), SalePaymentMethod.Cash, 112m) };

        var rows = ReportMath.BuildDaily(Monday, Tuesday, sales, returns, returnLines, payments, includeCost: true);

        rows[0].Should().BeEquivalentTo(new { SalesAmount = 112m, ReturnAmount = 0m, NetSales = 112m, Collected = 112m, Cost = 60m, Margin = 40m });
        rows[1].Should().BeEquivalentTo(new { Bills = 0, Returns = 1, ReturnAmount = 56m, NetSales = -56m, Collected = -56m, Cost = -30m, Margin = -20m });
    }

    [Fact]
    public void Daily_counts_money_on_the_day_it_was_paid_not_the_day_of_the_bill()
    {
        // A credit bill on Monday, settled on Tuesday.
        var sales = new[] { new SaleDayRow(At(Monday, 15), 500m, 0m, 446.43m, 53.57m, 500m, 300m) };
        var payments = new[] { new PaymentRow(At(Tuesday, 10), SalePaymentMethod.Upi, 500m) };

        var rows = ReportMath.BuildDaily(Monday, Tuesday, sales, [], [], payments, includeCost: false);

        rows[0].Should().BeEquivalentTo(new { SalesAmount = 500m, Collected = 0m });
        rows[1].Should().BeEquivalentTo(new { SalesAmount = 0m, Collected = 500m });
    }

    [Fact]
    public void Daily_hides_cost_and_margin_from_a_caller_who_may_not_see_cost()
    {
        var rows = ReportMath.BuildDaily(Monday, Monday, [new SaleDayRow(At(Monday, 10), 100m, 0m, 89.29m, 10.71m, 100m, 60m)], [], [], [], includeCost: false);

        rows[0].Cost.Should().BeNull();
        rows[0].Margin.Should().BeNull();
        ReportMath.Total(rows).Margin.Should().BeNull();
    }

    [Fact]
    public void Total_adds_the_days_up()
    {
        var sales = new[]
        {
            new SaleDayRow(At(Monday, 10), 100m, 0m, 89.29m, 10.71m, 100m, 60m),
            new SaleDayRow(At(Tuesday, 10), 200m, 0m, 178.57m, 21.43m, 200m, 100m),
        };
        var payments = new[] { new PaymentRow(At(Monday, 10), SalePaymentMethod.Cash, 100m), new PaymentRow(At(Tuesday, 10), SalePaymentMethod.Card, 150m) };

        var total = ReportMath.Total(ReportMath.BuildDaily(Monday, Tuesday, sales, [], [], payments, includeCost: true));

        total.Should().BeEquivalentTo(new SalesFigures { Bills = 2, SalesAmount = 300m, ReturnAmount = 0m, NetSales = 300m, Collected = 250m, Margin = 107.86m });
    }

    // ---- Product sales

    [Fact]
    public void Product_sales_are_net_of_returns_and_ranked_by_value()
    {
        var dolo = Guid.NewGuid();
        var pan = Guid.NewGuid();
        var sold = new[]
        {
            new ProductSoldRow(dolo, "Dolo 650", Bills: 4, Quantity: 40m, Taxable: 100m, Tax: 12m, Cost: 60m),
            new ProductSoldRow(pan, "Pan 40", Bills: 2, Quantity: 30m, Taxable: 300m, Tax: 36m, Cost: 210m),
        };
        var returnLines = new[] { new SaleReturnLineRow(At(Tuesday, 9), dolo, "Dolo 650", "3004", 12m, Quantity: 10m, Taxable: 25m, Tax: 3m, UnitCost: 1.5m) };

        var rows = ReportMath.BuildProductSales(sold, returnLines, includeCost: true);

        rows.Select(r => r.ProductName).Should().Equal("Pan 40", "Dolo 650");
        rows[0].Should().BeEquivalentTo(new { NetQuantity = 30m, Amount = 336m, Cost = 210m, Margin = 90m, MarginPercent = 30m });
        rows[1].Should().BeEquivalentTo(new { QuantitySold = 40m, QuantityReturned = 10m, NetQuantity = 30m, TaxableAmount = 75m, TaxAmount = 9m, Amount = 84m, Cost = 45m, Margin = 30m, MarginPercent = 40m });
    }

    [Fact]
    public void A_product_that_only_came_back_in_the_period_shows_as_a_negative()
    {
        var returnLines = new[] { new SaleReturnLineRow(At(Tuesday, 9), Guid.NewGuid(), "Alprax 0.25", "3004", 12m, Quantity: 5m, Taxable: 20m, Tax: 2.4m, UnitCost: 2m) };

        var rows = ReportMath.BuildProductSales([], returnLines, includeCost: true);

        rows.Should().ContainSingle().Which.Should().BeEquivalentTo(new { ProductName = "Alprax 0.25", Bills = 0, QuantitySold = 0m, QuantityReturned = 5m, NetQuantity = -5m, Amount = -22.4m, Cost = -10m, Margin = -10m });
        rows[0].MarginPercent.Should().BeNull();
    }

    // ---- Collections

    [Fact]
    public void Collections_are_by_day_and_mode_with_refunds_taken_off_the_mode_they_were_paid_in()
    {
        var payments = new[]
        {
            new PaymentRow(At(Monday, 10), SalePaymentMethod.Cash, 300m),
            new PaymentRow(At(Monday, 12), SalePaymentMethod.Cash, 200m),
            new PaymentRow(At(Monday, 13), SalePaymentMethod.Upi, 450m),
            new PaymentRow(At(Tuesday, 9), SalePaymentMethod.Card, 120m),
        };
        var returns = new[]
        {
            Return(At(Monday, 17), 50m, 6m, 56m, refund: 56m, SalePaymentMethod.Cash),
            Return(At(Tuesday, 11), 50m, 6m, 56m, refund: 0m),   // came off an unpaid bill: no money moved
        };

        var rows = ReportMath.BuildCollections(payments, returns);

        rows.Should().BeEquivalentTo(
        [
            new CollectionRow { Date = Monday, Method = SalePaymentMethod.Cash, Received = 500m, Refunded = 56m, Net = 444m },
            new CollectionRow { Date = Monday, Method = SalePaymentMethod.Upi, Received = 450m, Refunded = 0m, Net = 450m },
            new CollectionRow { Date = Tuesday, Method = SalePaymentMethod.Card, Received = 120m, Refunded = 0m, Net = 120m },
        ], options => options.WithStrictOrdering());
    }

    // ---- GST

    [Fact]
    public void Gst_within_the_state_is_split_into_equal_cgst_and_sgst_and_across_states_is_igst()
    {
        var rows = ReportMath.BuildGstRows(
        [
            new TaxGroupRow("3004", 12m, IsInterState: false, Taxable: 1000m, Tax: 120m),
            new TaxGroupRow("3004", 12m, IsInterState: true, Taxable: 500m, Tax: 60m),
            new TaxGroupRow("3004", 5m, IsInterState: false, Taxable: 200m, Tax: 10m),
        ]);

        rows.Should().BeEquivalentTo(
        [
            new GstRateRow { HsnCode = "3004", GstRatePercent = 5m, TaxableAmount = 200m, Cgst = 5m, Sgst = 5m, Igst = 0m, TaxAmount = 10m },
            new GstRateRow { HsnCode = "3004", GstRatePercent = 12m, TaxableAmount = 1500m, Cgst = 60m, Sgst = 60m, Igst = 60m, TaxAmount = 180m },
        ], options => options.WithStrictOrdering());
    }

    [Fact]
    public void Gst_halves_always_add_back_to_the_tax()
    {
        var row = ReportMath.BuildGstRows([new TaxGroupRow("3004", 12m, false, 89.29m, 10.71m)]).Single();

        row.Cgst.Should().Be(5.35m);
        row.Sgst.Should().Be(5.36m);
        (row.Cgst + row.Sgst).Should().Be(row.TaxAmount);
    }

    [Fact]
    public void Gst_treats_a_blank_hsn_code_as_none_and_drops_empty_groups()
    {
        var rows = ReportMath.BuildGstRows(
        [
            new TaxGroupRow(null, 12m, false, 100m, 12m),
            new TaxGroupRow("  ", 12m, false, 50m, 6m),
            new TaxGroupRow("3004", 0m, false, 0m, 0m),
        ]);

        rows.Should().ContainSingle().Which.Should().BeEquivalentTo(new { HsnCode = (string?)null, TaxableAmount = 150m, TaxAmount = 18m });
    }

    // ---- Stock valuation and non-moving stock

    private static readonly DateOnly Today = new(2026, 10, 14);

    private static ProductResponse Product(string name, decimal unitsPerPack = 10m, decimal mrp = 0m) => new() { Id = Guid.NewGuid(), ProductName = name, IsActive = true, UnitsPerPack = unitsPerPack, Mrp = mrp };

    private static ProductBatchResponse Batch(ProductResponse product, string batchNo, DateOnly expiry, decimal? purchaseRate, decimal? mrp, DateTime? createdAt = null)
        => new() { Id = Guid.NewGuid(), ProductId = product.Id, BatchNo = batchNo, ExpiryDate = expiry, IsActive = true, PurchaseRate = purchaseRate, Mrp = mrp, CreatedAt = createdAt ?? At(Today.AddDays(-10), 10) };

    [Fact]
    public void Valuation_prices_each_batch_at_its_own_rate_per_unit_and_sets_expired_stock_apart()
    {
        var dolo = Product("Dolo 650");
        var good = Batch(dolo, "DL1", Today.AddYears(1), purchaseRate: 15m, mrp: 30m);     // ₹1.50 / ₹3.00 a tablet
        var expired = Batch(dolo, "DL0", Today.AddDays(-1), purchaseRate: 12m, mrp: 28m);  // ₹1.20 / ₹2.80 a tablet
        var store = Guid.NewGuid();
        var stock = new[] { new StoreStockLine(store, dolo.Id, good.Id, 100m), new StoreStockLine(store, dolo.Id, expired.Id, 20m) };

        var valuation = ReportMath.BuildValuation(stock, new Dictionary<Guid, ProductResponse> { [dolo.Id] = dolo }, new[] { good, expired }.ToDictionary(b => b.Id), Today, includeCost: true);

        valuation.Rows.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Batches = 2, Quantity = 120m, CostValue = 174m, MrpValue = 356m, ExpiredQuantity = 20m, ExpiredCostValue = 24m });
        valuation.Should().BeEquivalentTo(new { TotalQuantity = 120m, TotalCostValue = 174m, TotalMrpValue = 356m, ExpiredCostValue = 24m });
    }

    [Fact]
    public void Valuation_keeps_cost_back_from_a_caller_who_may_not_see_it()
    {
        var dolo = Product("Dolo 650");
        var batch = Batch(dolo, "DL1", Today.AddYears(1), 15m, 30m);

        var valuation = ReportMath.BuildValuation([new StoreStockLine(Guid.NewGuid(), dolo.Id, batch.Id, 10m)], new Dictionary<Guid, ProductResponse> { [dolo.Id] = dolo }, new Dictionary<Guid, ProductBatchResponse> { [batch.Id] = batch }, Today, includeCost: false);

        valuation.TotalCostValue.Should().BeNull();
        valuation.Rows[0].CostValue.Should().BeNull();
        valuation.Rows[0].MrpValue.Should().Be(30m);
    }

    [Fact]
    public void Non_moving_is_measured_from_the_last_sale_or_from_stocking_when_never_sold()
    {
        var slow = Product("Slow seller");
        var fresh = Product("Sold last week");
        var unsold = Product("Never sold, old stock");
        var arrived = Product("Never sold, came yesterday");
        var batches = new[]
        {
            Batch(slow, "S1", Today.AddYears(1), 10m, 20m, At(Today.AddDays(-400), 10)),
            Batch(fresh, "F1", Today.AddYears(1), 10m, 20m, At(Today.AddDays(-400), 10)),
            Batch(unsold, "U1", Today.AddDays(40), 10m, 20m, At(Today.AddDays(-200), 10)),
            Batch(arrived, "A1", Today.AddYears(1), 10m, 20m, At(Today.AddDays(-1), 10)),
        };
        var store = Guid.NewGuid();
        var stock = batches.Select(b => new StoreStockLine(store, b.ProductId, b.Id, 50m)).ToList();
        var products = new[] { slow, fresh, unsold, arrived }.ToDictionary(p => p.Id);
        var lastSold = new Dictionary<Guid, DateTime> { [slow.Id] = At(Today.AddDays(-120), 12), [fresh.Id] = At(Today.AddDays(-7), 12) };

        var rows = ReportMath.BuildNonMoving(stock, products, batches.ToDictionary(b => b.Id), lastSold, Today, idleDays: 90, includeCost: true);

        rows.Select(r => r.ProductName).Should().Equal("Never sold, old stock", "Slow seller");
        rows[0].Should().BeEquivalentTo(new { LastSoldOn = (DateOnly?)null, IdleDays = 200, Quantity = 50m, CostValue = 50m, NearestExpiry = (DateOnly?)Today.AddDays(40) });
        rows[1].Should().BeEquivalentTo(new { LastSoldOn = (DateOnly?)Today.AddDays(-120), IdleDays = 120 });
    }
}

public class PharmacyReportServiceTests
{
    private readonly IPharmacyReportRepository _repository = Substitute.For<IPharmacyReportRepository>();
    private readonly IPharmacyStockControlRepository _stockRepository = Substitute.For<IPharmacyStockControlRepository>();
    private readonly IPharmacySettingsRepository _settings = Substitute.For<IPharmacySettingsRepository>();
    private readonly IStockBalanceService _stockBalanceService = Substitute.For<IStockBalanceService>();
    private readonly IProductService _productService = Substitute.For<IProductService>();
    private readonly IProductBatchService _productBatchService = Substitute.For<IProductBatchService>();
    private readonly PharmacyReportService _sut;

    private readonly DateOnly _today = PharmacyClock.Today();
    private readonly ProductResponse _product = new() { Id = Guid.NewGuid(), ProductName = "Dolo 650", IsActive = true, UnitsPerPack = 10m };

    public PharmacyReportServiceTests()
    {
        _sut = new PharmacyReportService(_repository, _stockRepository, _settings, _stockBalanceService, _productService, _productBatchService);

        _settings.GetAsync(Arg.Any<CancellationToken>()).Returns((PharmacySettings?)null);
        _repository.GetSaleDayRowsAsync(default, default, default, default).ReturnsForAnyArgs([]);
        _repository.GetSaleReturnsAsync(default, default, default, default).ReturnsForAnyArgs([]);
        _repository.GetSaleReturnLinesAsync(default, default, default, default).ReturnsForAnyArgs([]);
        _repository.GetPaymentsAsync(default, default, default, default).ReturnsForAnyArgs([]);
        _repository.GetProductSalesAsync(default, default, default, default).ReturnsForAnyArgs([]);
        _repository.GetSaleTaxGroupsAsync(default, default, default, default).ReturnsForAnyArgs([]);
        _repository.GetPurchaseTaxGroupsAsync(default, default, default, default).ReturnsForAnyArgs([]);
        _repository.GetPurchaseReturnTaxGroupsAsync(default, default, default, default).ReturnsForAnyArgs([]);
        _repository.GetCustomerDuesAsync(default, default).ReturnsForAnyArgs((0, 0m));
        _stockRepository.GetStockLinesAsync(default, default).ReturnsForAnyArgs([]);
        _stockBalanceService.GetSummariesAsync(default, default, default).ReturnsForAnyArgs([]);
        _productService.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([_product]);
        _productBatchService.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    private DateTime TodayAt(int hour) => PharmacyClock.StartOfDayUtc(_today).AddHours(hour);

    [Fact]
    public async Task Dashboard_reports_today_the_month_and_a_fortnight_of_days()
    {
        _repository.GetSaleDayRowsAsync(default, default, default, default).ReturnsForAnyArgs(
        [
            new SaleDayRow(TodayAt(10), 112m, 0m, 100m, 12m, 112m, 60m),
            new SaleDayRow(TodayAt(11), 56m, 0m, 50m, 6m, 56m, 30m),
            new SaleDayRow(TodayAt(10).AddDays(-1), 224m, 0m, 200m, 24m, 224m, 150m),
        ]);
        _repository.GetPaymentsAsync(default, default, default, default).ReturnsForAnyArgs(
        [
            new PaymentRow(TodayAt(10), SalePaymentMethod.Cash, 112m),
            new PaymentRow(TodayAt(11), SalePaymentMethod.Upi, 56m),
        ]);

        var dashboard = await _sut.GetDashboardAsync(null, includeCost: true, CancellationToken.None);

        dashboard.Date.Should().Be(_today);
        dashboard.Today.Should().BeEquivalentTo(new SalesFigures { Bills = 2, SalesAmount = 168m, ReturnAmount = 0m, NetSales = 168m, Collected = 168m, Margin = 60m });
        dashboard.Yesterday.Should().BeEquivalentTo(new { Bills = 1, SalesAmount = 224m, Margin = 50m });
        dashboard.Trend.Should().HaveCount(14);
        dashboard.Trend[^1].Date.Should().Be(_today);
        dashboard.TodayCollections.Should().BeEquivalentTo(
        [
            new PaymentModeTotal { Method = SalePaymentMethod.Cash, Amount = 112m },
            new PaymentModeTotal { Method = SalePaymentMethod.Upi, Amount = 56m },
        ]);
    }

    [Fact]
    public async Task Dashboard_counts_what_needs_attention_on_the_shelf()
    {
        var expired = new ProductBatchResponse { Id = Guid.NewGuid(), ProductId = _product.Id, BatchNo = "OLD", ExpiryDate = _today.AddDays(-3), IsActive = true, PurchaseRate = 10m };
        var soon = new ProductBatchResponse { Id = Guid.NewGuid(), ProductId = _product.Id, BatchNo = "SOON", ExpiryDate = _today.AddDays(20), IsActive = true, PurchaseRate = 10m };
        var good = new ProductBatchResponse { Id = Guid.NewGuid(), ProductId = _product.Id, BatchNo = "GOOD", ExpiryDate = _today.AddYears(1), IsActive = true, PurchaseRate = 10m };
        var store = Guid.NewGuid();

        _productBatchService.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([expired, soon, good]);
        _stockRepository.GetStockLinesAsync(default, default).ReturnsForAnyArgs(
        [
            new StoreStockLine(store, _product.Id, expired.Id, 10m),
            new StoreStockLine(store, _product.Id, soon.Id, 20m),
            new StoreStockLine(store, _product.Id, good.Id, 100m),
        ]);
        _stockBalanceService.GetSummariesAsync(StockSummaryFilter.NeedsReorder, Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(
        [
            new StockSummaryResponse { ProductId = Guid.NewGuid(), Status = StockStatus.LowStock },
            new StockSummaryResponse { ProductId = Guid.NewGuid(), Status = StockStatus.LowStock },
            new StockSummaryResponse { ProductId = Guid.NewGuid(), Status = StockStatus.OutOfStock },
        ]);
        _repository.GetCustomerDuesAsync(default, default).ReturnsForAnyArgs((3, 1250m));
        _repository.CountPendingIndentsAsync(default).ReturnsForAnyArgs(4);
        _repository.CountOpenPurchaseOrdersAsync(default, default).ReturnsForAnyArgs(2);
        _repository.GetSupplierPayableAsync(default).ReturnsForAnyArgs(48000m);

        var dashboard = await _sut.GetDashboardAsync(null, includeCost: true, CancellationToken.None);

        // Default alert window (no settings saved) is 30 days: only the 20-day batch is "soon".
        dashboard.Stock.Should().BeEquivalentTo(new StockAlertFigures
        {
            ProductsInStock = 1, LowStock = 2, OutOfStock = 1, ExpiredBatches = 1, ExpiringSoonBatches = 1,
            ExpiryAlertDays = PharmacyClock.ExpiringSoonWindowDays, StockValue = 130m, ExpiredValue = 10m,
        });
        dashboard.Pending.Should().BeEquivalentTo(new PendingWorkFigures { PendingIndents = 4, OpenPurchaseOrders = 2, BillsWithDues = 3, CustomerDues = 1250m, SupplierPayable = 48000m });
    }

    [Fact]
    public async Task Dashboard_without_cost_rights_shows_no_margin_stock_value_or_supplier_dues()
    {
        _repository.GetSaleDayRowsAsync(default, default, default, default).ReturnsForAnyArgs([new SaleDayRow(TodayAt(10), 112m, 0m, 100m, 12m, 112m, 60m)]);

        var dashboard = await _sut.GetDashboardAsync(null, includeCost: false, CancellationToken.None);

        dashboard.Today.Margin.Should().BeNull();
        dashboard.Stock.StockValue.Should().BeNull();
        dashboard.Pending.SupplierPayable.Should().BeNull();
        await _repository.DidNotReceiveWithAnyArgs().GetSupplierPayableAsync(default);
    }

    [Fact]
    public async Task Gst_summary_nets_returns_off_and_leaves_purchases_out_without_cost_rights()
    {
        _repository.GetSaleTaxGroupsAsync(default, default, default, default).ReturnsForAnyArgs([new TaxGroupRow("3004", 12m, false, 1000m, 120m)]);
        _repository.GetSaleReturnLinesAsync(default, default, default, default).ReturnsForAnyArgs([new SaleReturnLineRow(TodayAt(9), _product.Id, "Dolo 650", "3004", 12m, 5m, 100m, 12m, 6m)]);
        _repository.GetPurchaseTaxGroupsAsync(default, default, default, default).ReturnsForAnyArgs([new TaxGroupRow("3004", 12m, true, 2000m, 240m)]);
        _repository.GetPurchaseReturnTaxGroupsAsync(default, default, default, default).ReturnsForAnyArgs([new TaxGroupRow("3004", 12m, true, 500m, 60m)]);

        var full = await _sut.GetGstSummaryAsync(_today, _today, null, includeCost: true, CancellationToken.None);
        var salesOnly = await _sut.GetGstSummaryAsync(_today, _today, null, includeCost: false, CancellationToken.None);

        full.OutputTax.Should().Be(108m);
        full.InputTax.Should().Be(180m);
        full.Purchases.Should().ContainSingle().Which.Igst.Should().Be(240m);
        salesOnly.OutputTax.Should().Be(108m);
        salesOnly.InputTax.Should().BeNull();
        salesOnly.Purchases.Should().BeEmpty();
        salesOnly.PurchaseReturns.Should().BeEmpty();
    }

    [Fact]
    public async Task A_period_is_queried_from_the_start_of_its_first_local_day_to_the_end_of_its_last()
    {
        var from = new DateOnly(2026, 10, 1);
        var to = new DateOnly(2026, 10, 31);

        await _sut.GetCollectionsAsync(from, to, null, CancellationToken.None);

        await _repository.Received(1).GetPaymentsAsync(PharmacyClock.StartOfDayUtc(from), PharmacyClock.StartOfDayUtc(new DateOnly(2026, 11, 1)), null, Arg.Any<CancellationToken>());
    }
}
