using FluentAssertions;
using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Modules.Products.Application;
using HMS.Modules.Products.Contracts;
using HMS.Shared.Kernel;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Pharmacy.Application;

public class StockBalanceServiceTests
{
    private readonly IPharmacyStockBalanceRepository _balanceRepository = Substitute.For<IPharmacyStockBalanceRepository>();
    private readonly IPharmacyStoreRepository _storeRepository = Substitute.For<IPharmacyStoreRepository>();
    private readonly PharmacyStore _store = PharmacyStore.Create("MAIN", "Main Pharmacy", StoreType.Pharmacy, null);
    private readonly IProductService _productService = Substitute.For<IProductService>();
    private readonly IProductBatchService _productBatchService = Substitute.For<IProductBatchService>();
    private readonly IPharmacySettingsRepository _settingsRepository = Substitute.For<IPharmacySettingsRepository>();
    private readonly StockBalanceService _sut;

    private static readonly DateOnly Today = PharmacyClock.Today();

    private readonly Guid _paracetamolId = Guid.NewGuid();
    private readonly Guid _amoxicillinId = Guid.NewGuid();
    private readonly List<ProductBatchResponse> _batches = [];
    private readonly List<StockBalanceLine> _lines = [];

    public StockBalanceServiceTests()
    {
        _sut = new StockBalanceService(_balanceRepository, _storeRepository, _productService, _productBatchService, _settingsRepository);

        // One store, the default — what every tenant has until it adds more. Requests in
        // these tests name no store, so they resolve to it.
        _store.MakeDefault(null);
        _storeRepository.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns(_store);
        _storeRepository.GetByIdAsync(_store.Id, Arg.Any<CancellationToken>()).Returns(_store);

        _balanceRepository.GetLinesAsync(Arg.Any<IReadOnlyCollection<Guid>?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var productIds = call.Arg<IReadOnlyCollection<Guid>?>();
                return _lines.Where(l => productIds is null || productIds.Contains(l.ProductId)).ToList();
            });
        _productBatchService.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(call => _batches.Where(b => call.Arg<IReadOnlyCollection<Guid>>().Contains(b.Id)).ToList());
        _productService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<ProductResponse>.Success(new ProductResponse { Id = call.Arg<Guid>(), SellingPrice = 20m, IsActive = true }));
    }

    private Guid AddStock(Guid productId, string batchNo, decimal quantity, DateOnly expiry, bool isActive = true, decimal? mrp = null)
    {
        var batchId = Guid.NewGuid();
        _batches.Add(new ProductBatchResponse { Id = batchId, ProductId = productId, BatchNo = batchNo, ExpiryDate = expiry, IsActive = isActive, Mrp = mrp });
        _lines.Add(new StockBalanceLine(productId, batchId, quantity));
        return batchId;
    }

    private ProductStockProfileResponse Profile(Guid id, string name, decimal reorderLevel) =>
        new() { Id = id, Sku = name.ToUpperInvariant(), ProductName = name, ReorderLevel = reorderLevel };

    [Fact]
    public async Task GetSummaryPagedAsync_Unfiltered_LetsProductsPageAndLoadsOnlyThatPagesBalances()
    {
        AddStock(_paracetamolId, "B-1", 30m, Today.AddMonths(6));
        AddStock(_paracetamolId, "B-2", 20m, Today.AddMonths(9));
        AddStock(_amoxicillinId, "B-9", 5m, Today.AddMonths(6));

        // Products holds 240 matches; this is page 2 of them, carrying only Paracetamol.
        _productService.GetPagedAsync(Arg.Any<ProductListQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductResponse>(
                [new ProductResponse { Id = _paracetamolId, Sku = "PARA-500", ProductName = "Paracetamol 500mg", ReorderLevel = 40m, IsActive = true }],
                page: 2, pageSize: 20, totalCount: 240));

        var result = await _sut.GetSummaryPagedAsync(new StockSummaryListQuery { Page = 2, PageSize = 20, Search = "para" }, CancellationToken.None);

        result.TotalCount.Should().Be(240);
        result.Items.Should().ContainSingle();
        result.Items[0].ProductName.Should().Be("Paracetamol 500mg");
        result.Items[0].AvailableQuantity.Should().Be(50m);
        result.Items[0].Status.Should().Be(StockStatus.InStock);

        await _productService.Received(1).GetPagedAsync(
            Arg.Is<ProductListQuery>(q => q.Page == 2 && q.PageSize == 20 && q.Search == "para" && q.IsActive == true),
            Arg.Any<CancellationToken>());
        await _balanceRepository.Received(1).GetLinesAsync(
            Arg.Is<IReadOnlyCollection<Guid>?>(ids => ids != null && ids.Count == 1 && ids.Contains(_paracetamolId)),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
        await _productService.DidNotReceive().GetStockProfilesAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSummaryPagedAsync_NeedsReorder_KeepsOnlyLowAndOutOfStockProductsAndCountsThem()
    {
        var ibuprofenId = Guid.NewGuid();
        var cetirizineId = Guid.NewGuid();
        AddStock(_paracetamolId, "B-1", 100m, Today.AddMonths(6));
        AddStock(_amoxicillinId, "B-2", 5m, Today.AddMonths(6));
        AddStock(ibuprofenId, "B-3", 40m, Today.AddDays(-3));

        _productService.GetStockProfilesAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductStockProfileResponse>
            {
                Profile(_amoxicillinId, "Amoxicillin 250mg", reorderLevel: 20m),
                Profile(cetirizineId, "Cetirizine 10mg", reorderLevel: 0m),
                Profile(ibuprofenId, "Ibuprofen 400mg", reorderLevel: 10m),
                Profile(_paracetamolId, "Paracetamol 500mg", reorderLevel: 20m),
            });

        var result = await _sut.GetSummaryPagedAsync(
            new StockSummaryListQuery { Filter = StockSummaryFilter.NeedsReorder }, CancellationToken.None);

        // Amoxicillin is low; Ibuprofen's only stock has expired, so it is out. Paracetamol is
        // well stocked and Cetirizine has never been stocked — neither belongs on a reorder list.
        result.Items.Select(s => s.ProductName).Should().Equal("Amoxicillin 250mg", "Ibuprofen 400mg");
        result.Items.Select(s => s.Status).Should().Equal(StockStatus.LowStock, StockStatus.OutOfStock);
        result.TotalCount.Should().Be(2);

        await _balanceRepository.Received(1).GetLinesAsync(Arg.Is<IReadOnlyCollection<Guid>?>(ids => ids == null), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
        await _productService.DidNotReceive().GetPagedAsync(Arg.Any<ProductListQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSummaryPagedAsync_Filtered_PagesTheMatchesNotTheCatalog()
    {
        var profiles = new List<ProductStockProfileResponse>();
        for (var i = 1; i <= 5; i++)
        {
            var productId = Guid.NewGuid();
            profiles.Add(Profile(productId, $"Product {i}", reorderLevel: 0m));
            AddStock(productId, $"B-{i}", 10m, Today.AddDays(-1));
        }

        _productService.GetStockProfilesAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(profiles);

        var result = await _sut.GetSummaryPagedAsync(
            new StockSummaryListQuery { Filter = StockSummaryFilter.Expired, Page = 2, PageSize = 2 }, CancellationToken.None);

        result.TotalCount.Should().Be(5);
        result.Items.Select(s => s.ProductName).Should().Equal("Product 3", "Product 4");
    }

    [Fact]
    public async Task GetAvailableBatchesAsync_ReturnsOnlyDispensableBatches_EarliestExpiryFirst()
    {
        var later = AddStock(_paracetamolId, "B-LATER", 30m, Today.AddMonths(8));
        var soonest = AddStock(_paracetamolId, "B-SOON", 12m, Today.AddDays(10));
        AddStock(_paracetamolId, "B-EXPIRED", 40m, Today.AddDays(-1));
        AddStock(_paracetamolId, "B-EMPTY", 0m, Today.AddMonths(3));
        AddStock(_paracetamolId, "B-INACTIVE", 25m, Today.AddMonths(3), isActive: false);
        AddStock(_amoxicillinId, "B-OTHER", 99m, Today.AddDays(1));

        var batches = await _sut.GetAvailableBatchesAsync(_paracetamolId, storeId: null, CancellationToken.None);

        batches.Select(b => b.ProductBatchId).Should().Equal(soonest, later);
        batches[0].BatchNo.Should().Be("B-SOON");
        batches[0].QuantityOnHand.Should().Be(12m);
        batches[0].IsExpiringSoon.Should().BeTrue();
        batches[1].IsExpiringSoon.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailableBatchesAsync_PricesEachBatchPerUnit_FromItsOwnMrpWhereItHasOne()
    {
        // A strip of 10. One batch carries its own MRP of ₹24, the other has none and falls
        // back to the product's ₹21 — so the same tablet is ₹2.40 from one batch, ₹2.10 from the other.
        _productService.GetByIdAsync(_paracetamolId, Arg.Any<CancellationToken>())
            .Returns(Result<ProductResponse>.Success(new ProductResponse { Id = _paracetamolId, UnitsPerPack = 10m, SellingPrice = 21m, IsActive = true }));
        AddStock(_paracetamolId, "B-PRICED", 30m, Today.AddMonths(3), mrp: 24m);
        AddStock(_paracetamolId, "B-PLAIN", 30m, Today.AddMonths(6));

        var batches = await _sut.GetAvailableBatchesAsync(_paracetamolId, storeId: null, CancellationToken.None);

        batches.Select(b => (b.BatchNo, b.Mrp, b.UnitPrice)).Should().Equal(("B-PRICED", 24m, 2.4m), ("B-PLAIN", null, 2.1m));
    }

    [Fact]
    public async Task GetAvailableBatchesAsync_ReadsOnlyTheResolvedStoresStock()
    {
        AddStock(_paracetamolId, "B-1", 30m, Today.AddMonths(3));

        await _sut.GetAvailableBatchesAsync(_paracetamolId, storeId: null, CancellationToken.None);

        await _balanceRepository.Received(1).GetLinesAsync(
            Arg.Any<IReadOnlyCollection<Guid>?>(), _store.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAvailableBatchesAsync_ForAnUnknownOrInactiveStore_ReturnsEmpty()
    {
        AddStock(_paracetamolId, "B-1", 30m, Today.AddMonths(3));

        var batches = await _sut.GetAvailableBatchesAsync(_paracetamolId, storeId: Guid.NewGuid(), CancellationToken.None);

        batches.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableBatchesAsync_WhenProductHasNoStock_ReturnsEmpty()
    {
        var batches = await _sut.GetAvailableBatchesAsync(_paracetamolId, storeId: null, CancellationToken.None);

        batches.Should().BeEmpty();
    }
}
