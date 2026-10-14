using FluentAssertions;
using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Products.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.Pharmacy.Application;

public class StockSummaryCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 10, 13);
    private const int Window = 30;

    private readonly Guid _productId = Guid.NewGuid();
    private readonly Dictionary<Guid, ProductBatchResponse> _batches = [];
    private readonly List<StockBalanceLine> _lines = [];

    private ProductStockProfileResponse Product(decimal reorderLevel = 0m) => new()
    {
        Id = _productId,
        Sku = "PARA-500",
        ProductName = "Paracetamol 500mg",
        GenericName = "Paracetamol",
        ReorderLevel = reorderLevel,
    };

    private void AddBatch(decimal quantity, DateOnly expiry, bool isActive = true)
    {
        var batchId = Guid.NewGuid();
        _batches[batchId] = new ProductBatchResponse { Id = batchId, ProductId = _productId, BatchNo = $"B-{_batches.Count + 1}", ExpiryDate = expiry, IsActive = isActive };
        _lines.Add(new StockBalanceLine(_productId, batchId, quantity));
    }

    private StockSummaryResponse Build(decimal reorderLevel = 0m) =>
        StockSummaryCalculator.Build(Product(reorderLevel), _lines, _batches, Today, Window);

    [Fact]
    public void Build_SumsAvailableStockAcrossBatches_SoSeveralSmallBatchesAreNotEachJudgedLow()
    {
        // Three batches of 8 against a reorder level of 10: every batch alone is under the
        // reorder level, but the product as a whole (24) is comfortably above it.
        AddBatch(8m, Today.AddMonths(6));
        AddBatch(8m, Today.AddMonths(9));
        AddBatch(8m, Today.AddMonths(12));

        var summary = Build(reorderLevel: 10m);

        summary.AvailableQuantity.Should().Be(24m);
        summary.BatchCount.Should().Be(3);
        summary.Status.Should().Be(StockStatus.InStock);
    }

    [Fact]
    public void Build_AcrossStores_AddsTheSameBatchUpButCountsItOnce()
    {
        // Rolled up over every store, a batch held in two of them arrives as two lines.
        AddBatch(8m, Today.AddMonths(6));
        _lines.Add(_lines[0] with { QuantityOnHand = 5m });

        var summary = Build();

        summary.AvailableQuantity.Should().Be(13m);
        summary.BatchCount.Should().Be(1);
    }

    [Fact]
    public void Build_WhenAvailableIsAtOrBelowReorderLevel_IsLowStock()
    {
        AddBatch(10m, Today.AddMonths(6));

        Build(reorderLevel: 10m).Status.Should().Be(StockStatus.LowStock);
    }

    [Fact]
    public void Build_WithNoReorderLevel_IsNeverLowStock()
    {
        AddBatch(1m, Today.AddMonths(6));

        Build(reorderLevel: 0m).Status.Should().Be(StockStatus.InStock);
    }

    [Fact]
    public void Build_ExcludesExpiredStockFromAvailable_SoAProductWithOnlyExpiredStockIsOutOfStock()
    {
        AddBatch(50m, Today.AddDays(-1));

        var summary = Build();

        summary.AvailableQuantity.Should().Be(0m);
        summary.ExpiredQuantity.Should().Be(50m);
        summary.NearestExpiry.Should().BeNull();
        summary.Status.Should().Be(StockStatus.OutOfStock);
    }

    [Fact]
    public void Build_TreatsABatchExpiringTodayAsStillAvailable()
    {
        // Matches DispenseService, which only rejects a batch once its expiry date is past.
        AddBatch(5m, Today);

        var summary = Build();

        summary.AvailableQuantity.Should().Be(5m);
        summary.ExpiredQuantity.Should().Be(0m);
        summary.ExpiringSoonQuantity.Should().Be(5m);
    }

    [Fact]
    public void Build_CountsOnlyStockInsideTheWindowAsExpiringSoon_AndReportsTheNearestExpiry()
    {
        AddBatch(4m, Today.AddDays(Window));
        AddBatch(6m, Today.AddDays(Window + 1));

        var summary = Build();

        summary.AvailableQuantity.Should().Be(10m);
        summary.ExpiringSoonQuantity.Should().Be(4m);
        summary.NearestExpiry.Should().Be(Today.AddDays(Window));
    }

    [Fact]
    public void Build_HoldsBackStockInInactiveBatches()
    {
        AddBatch(7m, Today.AddMonths(6), isActive: false);
        AddBatch(3m, Today.AddMonths(6));

        var summary = Build();

        summary.AvailableQuantity.Should().Be(3m);
        summary.OnHoldQuantity.Should().Be(7m);
        summary.BatchCount.Should().Be(2);
    }

    [Fact]
    public void Build_IgnoresZeroBalancesAndBalancesWhoseBatchNoLongerResolves()
    {
        AddBatch(0m, Today.AddMonths(6));
        _lines.Add(new StockBalanceLine(_productId, Guid.NewGuid(), 99m));

        var summary = Build();

        summary.AvailableQuantity.Should().Be(0m);
        summary.BatchCount.Should().Be(0);
        // Still OutOfStock rather than NotStocked: the balance rows show it has been stocked.
        summary.Status.Should().Be(StockStatus.OutOfStock);
    }

    [Fact]
    public void Build_WhenNeverReceived_IsNotStockedUnlessAReorderLevelSaysItShouldBe()
    {
        Build(reorderLevel: 0m).Status.Should().Be(StockStatus.NotStocked);
        Build(reorderLevel: 20m).Status.Should().Be(StockStatus.OutOfStock);
    }

    [Theory]
    [InlineData(StockStatus.LowStock, true)]
    [InlineData(StockStatus.OutOfStock, true)]
    [InlineData(StockStatus.InStock, false)]
    [InlineData(StockStatus.NotStocked, false)]
    public void Matches_NeedsReorder_IsLowOrOutOfStockOnly(StockStatus status, bool expected)
    {
        var summary = new StockSummaryResponse { Status = status };

        StockSummaryCalculator.Matches(summary, StockSummaryFilter.NeedsReorder).Should().Be(expected);
    }

    [Fact]
    public void Matches_ExpiryFilters_FollowTheirOwnQuantities()
    {
        var expiringSoon = new StockSummaryResponse { ExpiringSoonQuantity = 1m };
        var expired = new StockSummaryResponse { ExpiredQuantity = 1m };

        StockSummaryCalculator.Matches(expiringSoon, StockSummaryFilter.ExpiringSoon).Should().BeTrue();
        StockSummaryCalculator.Matches(expiringSoon, StockSummaryFilter.Expired).Should().BeFalse();
        StockSummaryCalculator.Matches(expired, StockSummaryFilter.Expired).Should().BeTrue();
        StockSummaryCalculator.Matches(expired, StockSummaryFilter.ExpiringSoon).Should().BeFalse();
    }
}
