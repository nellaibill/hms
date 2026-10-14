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

public class StockControlServiceTests
{
    private readonly IPharmacyStockControlRepository _repository = Substitute.For<IPharmacyStockControlRepository>();
    private readonly IPharmacyStoreRepository _storeRepository = Substitute.For<IPharmacyStoreRepository>();
    private readonly IPharmacySettingsRepository _settings = Substitute.For<IPharmacySettingsRepository>();
    private readonly FakePharmacyUnitOfWork _unitOfWork = new();
    private readonly StockFixture _stock = new();
    private readonly IProductService _productService = Substitute.For<IProductService>();
    private readonly IProductBatchService _productBatchService = Substitute.For<IProductBatchService>();
    private readonly PharmacyStore _main;
    private readonly PharmacyStore _ward = PharmacyStore.Create("WARD", "Ward A sub-store", StoreType.SubStore, null);
    private readonly StockControlService _sut;

    private readonly ProductResponse _product = new() { Id = Guid.NewGuid(), ProductName = "Dolo 650", IsActive = true, UnitsPerPack = 10m, RackLocation = "A-12" };
    private readonly ProductBatchResponse _batch;
    private readonly ProductBatchResponse _oldBatch;
    private PharmacyStockAdjustment? _savedAdjustment;
    private PharmacyStockCount? _savedCount;

    public StockControlServiceTests()
    {
        _main = StockFixture.DefaultStore(_storeRepository);
        _storeRepository.GetByIdAsync(_ward.Id, Arg.Any<CancellationToken>()).Returns(_ward);
        _storeRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([_main, _ward]);

        _batch = new ProductBatchResponse { Id = Guid.NewGuid(), ProductId = _product.Id, BatchNo = "DL2501", ExpiryDate = PharmacyClock.Today().AddYears(1), IsActive = true, PurchaseRate = 14.5m };
        _oldBatch = new ProductBatchResponse { Id = Guid.NewGuid(), ProductId = _product.Id, BatchNo = "OLD1", ExpiryDate = PharmacyClock.Today().AddDays(20), IsActive = true, PurchaseRate = 14.5m };

        _sut = new StockControlService(_repository, _storeRepository, _settings, _unitOfWork, _stock.Poster, _productService, _productBatchService);

        _settings.GetAsync(Arg.Any<CancellationToken>()).Returns((PharmacySettings?)null);
        _productService.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([_product]);
        _productBatchService.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(call => new[] { _batch, _oldBatch }.Where(b => call.Arg<IReadOnlyCollection<Guid>>().Contains(b.Id)).ToList());
        _repository.GetBatchSuppliersAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, (Guid, string)>());

        _repository.When(r => r.AddAdjustmentAsync(Arg.Any<PharmacyStockAdjustment>(), Arg.Any<CancellationToken>())).Do(call => _savedAdjustment = call.Arg<PharmacyStockAdjustment>());
        _repository.When(r => r.AddCountAsync(Arg.Any<PharmacyStockCount>(), Arg.Any<CancellationToken>())).Do(call => _savedCount = call.Arg<PharmacyStockCount>());
        _repository.GetCountAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(_ => _savedCount);

        _stock.Seed(_main.Id, _product.Id, _batch.Id, 100m);
        Books((_batch.Id, 100m));
    }

    /// <summary>What the repository reports the main store as holding.</summary>
    private void Books(params (Guid BatchId, decimal Quantity)[] lines)
        => _repository.GetStockLinesAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(lines.Select(l => new StoreStockLine(_main.Id, _product.Id, l.BatchId, l.Quantity)).ToList());

    private static CreateStockAdjustmentRequest Adjust(Guid productId, Guid batchId, decimal quantity, AdjustmentReason reason = AdjustmentReason.Damaged) => new()
    {
        Reason = reason,
        Remarks = "Bottle broken",
        Lines = [new StockAdjustmentLineRequest { ProductId = productId, ProductBatchId = batchId, Quantity = quantity }],
    };

    // ---- Adjustments

    [Fact]
    public async Task CreateAdjustmentAsync_ANegativeQuantity_TakesStockOutAndValuesTheLoss()
    {
        var result = await _sut.CreateAdjustmentAsync(Adjust(_product.Id, _batch.Id, -10m), null, includeCost: true, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AdjustmentNo.Should().Be("ADJ-2026-000001");
        _stock.OnHand(_main.Id, _product.Id, _batch.Id).Should().Be(90m);
        _stock.Posted.Single().TransactionType.Should().Be(TransactionType.AdjustmentOut);

        // Ten tablets at 1.45 each.
        result.Value.TotalValue.Should().Be(-14.50m);
    }

    [Fact]
    public async Task CreateAdjustmentAsync_APositiveQuantity_AddsStock()
    {
        await _sut.CreateAdjustmentAsync(Adjust(_product.Id, _batch.Id, 5m, AdjustmentReason.Excess), null, includeCost: true, CancellationToken.None);

        _stock.OnHand(_main.Id, _product.Id, _batch.Id).Should().Be(105m);
        _stock.Posted.Single().TransactionType.Should().Be(TransactionType.AdjustmentIn);
        _stock.Posted.Single().SourceType.Should().Be(StockSourceType.Adjustment);
    }

    [Fact]
    public async Task CreateAdjustmentAsync_TakingOutMoreThanIsThere_IsRefused()
    {
        var result = await _sut.CreateAdjustmentAsync(Adjust(_product.Id, _batch.Id, -101m), null, includeCost: true, CancellationToken.None);

        result.ErrorCode.Should().Be("PHARMACY.INSUFFICIENT_STOCK");
        _unitOfWork.Saves.Should().Be(0);
    }

    [Fact]
    public async Task CreateAdjustmentAsync_WithoutTheRightToSeeCost_HidesTheValue()
    {
        var result = await _sut.CreateAdjustmentAsync(Adjust(_product.Id, _batch.Id, -10m), null, includeCost: false, CancellationToken.None);

        result.Value!.TotalValue.Should().BeNull();
        result.Value.Lines.Single().Value.Should().BeNull();
    }

    // ---- Transfers

    [Fact]
    public async Task CreateTransferAsync_MovesStockOutOfOneStoreAndIntoTheOther()
    {
        var result = await _sut.CreateTransferAsync(
            new CreateStockTransferRequest { FromStoreId = _main.Id, ToStoreId = _ward.Id, Lines = [new StockTransferLineRequest { ProductId = _product.Id, ProductBatchId = _batch.Id, Quantity = 30m }] },
            null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TransferNo.Should().Be("ST-2026-000001");
        _stock.OnHand(_main.Id, _product.Id, _batch.Id).Should().Be(70m);
        _stock.OnHand(_ward.Id, _product.Id, _batch.Id).Should().Be(30m);
        _stock.Posted.Select(t => t.TransactionType).Should().Equal(TransactionType.TransferOut, TransactionType.TransferIn);
        _stock.Posted.Should().OnlyContain(t => t.SourceNo == "ST-2026-000001");
    }

    [Fact]
    public async Task CreateTransferAsync_MoreThanTheSendingStoreHolds_MovesNothing()
    {
        var result = await _sut.CreateTransferAsync(
            new CreateStockTransferRequest { FromStoreId = _main.Id, ToStoreId = _ward.Id, Lines = [new StockTransferLineRequest { ProductId = _product.Id, ProductBatchId = _batch.Id, Quantity = 130m }] },
            null, CancellationToken.None);

        result.ErrorCode.Should().Be("PHARMACY.INSUFFICIENT_STOCK");
        _stock.OnHand(_ward.Id, _product.Id, _batch.Id).Should().Be(0m);
    }

    [Fact]
    public async Task CreateTransferAsync_ToTheSameStore_IsRefused()
    {
        var result = await _sut.CreateTransferAsync(
            new CreateStockTransferRequest { FromStoreId = _main.Id, ToStoreId = _main.Id, Lines = [new StockTransferLineRequest { ProductId = _product.Id, ProductBatchId = _batch.Id, Quantity = 1m }] },
            null, CancellationToken.None);

        result.ErrorCode.Should().Be("PHARMACY.INVALID_STORE");
    }

    // ---- Stock count

    [Fact]
    public async Task StartCountAsync_ListsEveryBatchTheBooksSayTheStoreHolds()
    {
        var result = await _sut.StartCountAsync(new StartStockCountRequest(), null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CountNo.Should().Be("SC-2026-000001");
        var line = result.Value.Lines.Single();
        line.BookQuantity.Should().Be(100m);
        line.RackLocation.Should().Be("A-12");
        line.CountedQuantity.Should().BeNull();
    }

    [Fact]
    public async Task StartCountAsync_WhileAnotherIsOpenInTheStore_IsRefused()
    {
        _repository.HasOpenCountAsync(_main.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.StartCountAsync(new StartStockCountRequest(), null, CancellationToken.None);

        result.ErrorCode.Should().Be("PHARMACY.INVALID_STATE");
    }

    [Fact]
    public async Task PostCountAsync_TurnsAShortageIntoAnAdjustment()
    {
        var count = (await _sut.StartCountAsync(new StartStockCountRequest(), null, CancellationToken.None)).Value!;
        await _sut.SaveCountAsync(count.Id, new SaveStockCountRequest { Entries = [new StockCountEntry { LineId = count.Lines[0].Id, CountedQuantity = 96m }] }, null, CancellationToken.None);

        var result = await _sut.PostCountAsync(count.Id, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.Status.Should().Be(StockCountStatus.Posted);
        result.Value.AdjustmentId.Should().Be(_savedAdjustment!.Id);
        _savedAdjustment.Reason.Should().Be(AdjustmentReason.StockCount);
        _savedAdjustment.Lines.Single().Quantity.Should().Be(-4m);
        _stock.OnHand(_main.Id, _product.Id, _batch.Id).Should().Be(96m);
    }

    [Fact]
    public async Task PostCountAsync_MeasuresAgainstTheBooksAsTheyAreNow_NotAsTheyWereAtTheStart()
    {
        var count = (await _sut.StartCountAsync(new StartStockCountRequest(), null, CancellationToken.None)).Value!;

        // Ten were sold while the count was open, then 90 were found on the shelf: nothing is missing.
        _stock.Seed(_main.Id, _product.Id, _batch.Id, 90m);
        Books((_batch.Id, 90m));
        await _sut.SaveCountAsync(count.Id, new SaveStockCountRequest { Entries = [new StockCountEntry { LineId = count.Lines[0].Id, CountedQuantity = 90m }] }, null, CancellationToken.None);

        var result = await _sut.PostCountAsync(count.Id, null, CancellationToken.None);

        result.Value!.Status.Should().Be(StockCountStatus.Posted);
        result.Value.AdjustmentId.Should().BeNull("the shelf matched the books");
        _savedAdjustment.Should().BeNull();
    }

    [Fact]
    public async Task PostCountAsync_WithNothingCounted_IsRefused()
    {
        var count = (await _sut.StartCountAsync(new StartStockCountRequest(), null, CancellationToken.None)).Value!;

        var result = await _sut.PostCountAsync(count.Id, null, CancellationToken.None);

        result.ErrorCode.Should().Be("PHARMACY.INVALID_STATE");
    }

    [Fact]
    public async Task PostCountAsync_LeavesLinesThatWereNeverCountedAlone()
    {
        _stock.Seed(_main.Id, _product.Id, _oldBatch.Id, 20m);
        Books((_batch.Id, 100m), (_oldBatch.Id, 20m));
        var count = (await _sut.StartCountAsync(new StartStockCountRequest(), null, CancellationToken.None)).Value!;
        var counted = count.Lines.Single(l => l.BatchNo == "DL2501");
        await _sut.SaveCountAsync(count.Id, new SaveStockCountRequest { Entries = [new StockCountEntry { LineId = counted.Id, CountedQuantity = 99m }] }, null, CancellationToken.None);

        await _sut.PostCountAsync(count.Id, null, CancellationToken.None);

        _savedAdjustment!.Lines.Should().ContainSingle().Which.BatchNo.Should().Be("DL2501");
        _stock.OnHand(_main.Id, _product.Id, _oldBatch.Id).Should().Be(20m);
    }

    // ---- Expiry

    [Fact]
    public async Task GetExpiringAsync_ListsBatchesInsideTheWindow_SoonestFirst_WithTheirValue()
    {
        Books((_batch.Id, 100m), (_oldBatch.Id, 20m));

        var expiring = await _sut.GetExpiringAsync(null, days: 30, includeCost: true, CancellationToken.None);

        var row = expiring.Should().ContainSingle().Which;
        row.BatchNo.Should().Be("OLD1");
        row.DaysLeft.Should().Be(20);
        row.Value.Should().Be(29m);
        row.StoreName.Should().Be("Main Pharmacy");
    }

    [Fact]
    public async Task GetExpiringAsync_WithNoWindowGiven_UsesThePharmacysOwnSetting()
    {
        Books((_oldBatch.Id, 20m));
        var settings = PharmacySettings.Create(null);
        settings.Update(null, null, null, null, null, null, BillPrintFormat.A5, 100m, expiryAlertDays: 10, null);
        _settings.GetAsync(Arg.Any<CancellationToken>()).Returns(settings);

        // The batch has 20 days left; a 10-day window does not reach it.
        (await _sut.GetExpiringAsync(null, days: null, includeCost: true, CancellationToken.None)).Should().BeEmpty();
    }

    [Fact]
    public async Task GetExpiringAsync_SaysWhoTheBatchWasBoughtFrom()
    {
        Books((_oldBatch.Id, 20m));
        var supplierId = Guid.NewGuid();
        _repository.GetBatchSuppliersAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, (Guid, string)> { [_oldBatch.Id] = (supplierId, "Sri Medicals") });

        var row = (await _sut.GetExpiringAsync(null, 30, includeCost: false, CancellationToken.None)).Single();

        row.SupplierName.Should().Be("Sri Medicals");
        row.Value.Should().BeNull();
    }
}

public class StockSummaryExpiryWindowTests
{
    [Fact]
    public void Build_UsesTheWindowItIsGiven()
    {
        var productId = Guid.NewGuid();
        var batchId = Guid.NewGuid();
        var today = new DateOnly(2026, 10, 14);
        var product = new ProductStockProfileResponse { Id = productId, ProductName = "Dolo 650", Sku = "D650" };
        var lines = new[] { new StockBalanceLine(productId, batchId, 40m) };
        var batches = new Dictionary<Guid, ProductBatchResponse> { [batchId] = new() { Id = batchId, ProductId = productId, BatchNo = "B1", ExpiryDate = today.AddDays(60), IsActive = true } };

        StockSummaryCalculator.Build(product, lines, batches, today).ExpiringSoonQuantity.Should().Be(0m, "60 days is outside the default 30");
        StockSummaryCalculator.Build(product, lines, batches, today, expiringSoonWindowDays: 90).ExpiringSoonQuantity.Should().Be(40m);
    }
}
