using FluentAssertions;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
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

public class ReorderSuggestionTests
{
    [Theory]
    // available, reorder, max, pack, onOrder -> packs
    [InlineData(20, 50, 200, 10, 0, 18)]   // fill to the maximum: 180 units = 18 strips
    [InlineData(20, 50, 0, 10, 0, 8)]      // no maximum set: fill to twice the reorder level
    [InlineData(20, 50, 200, 10, 10, 8)]   // 10 strips already on order come off
    [InlineData(20, 50, 200, 10, 18, 0)]   // open orders cover it
    [InlineData(0, 30, 0, 15, 0, 4)]       // 60 units needed, strips of 15
    [InlineData(5, 30, 0, 15, 0, 4)]       // 55 units rounds up to 4 strips
    [InlineData(250, 50, 200, 10, 0, 0)]   // already above the target
    public void SuggestPacks_FillsToTheTargetInWholePacks(decimal available, decimal reorder, decimal max, decimal pack, decimal onOrder, decimal expected)
    {
        PurchaseOrderService.SuggestPacks(available, reorder, max, pack, onOrder).Should().Be(expected);
    }
}

public class PurchaseOrderDomainTests
{
    private static PharmacyPurchaseOrder NewOrder()
        => PharmacyPurchaseOrder.Create("PO-2026-000001", Guid.NewGuid(), Guid.NewGuid(), "Sri Medicals", new DateOnly(2026, 10, 14), null, null, null);

    [Fact]
    public void RecordReceipt_PartOfALine_LeavesTheOrderPartlyReceived()
    {
        var order = NewOrder();
        var dolo = Guid.NewGuid();
        var pan = Guid.NewGuid();
        order.AddLine(dolo, "Dolo 650", 10m, 14.5m, 12m);
        order.AddLine(pan, "Pan 40", 5m, 110m, 12m);
        order.MarkOrdered(null);

        order.RecordReceipt(dolo, 10m, null);

        order.Status.Should().Be(PurchaseOrderStatus.PartlyReceived);
        order.Lines[0].PendingQuantity.Should().Be(0m);
        order.Lines[1].PendingQuantity.Should().Be(5m);
    }

    [Fact]
    public void RecordReceipt_OfEverything_CompletesTheOrder()
    {
        var order = NewOrder();
        var dolo = Guid.NewGuid();
        order.AddLine(dolo, "Dolo 650", 10m, 14.5m, 12m);
        order.MarkOrdered(null);

        order.RecordReceipt(dolo, 6m, null);
        order.RecordReceipt(dolo, 4m, null);

        order.Status.Should().Be(PurchaseOrderStatus.Received);
        order.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void RecordReceipt_OfAProductNotOnTheOrder_ChangesNothing()
    {
        var order = NewOrder();
        order.AddLine(Guid.NewGuid(), "Dolo 650", 10m, 14.5m, 12m);
        order.MarkOrdered(null);

        order.RecordReceipt(Guid.NewGuid(), 3m, null);

        order.Status.Should().Be(PurchaseOrderStatus.Ordered);
    }

    [Fact]
    public void ASentOrder_CanNoLongerBeEdited()
    {
        var order = NewOrder();
        order.AddLine(Guid.NewGuid(), "Dolo 650", 10m, 14.5m, 12m);
        order.MarkOrdered(null);

        var edit = () => order.AddLine(Guid.NewGuid(), "Pan 40", 5m, 110m, 12m);

        edit.Should().Throw<InvalidOperationException>().WithMessage("*has been sent*");
    }

    [Fact]
    public void AnEmptyOrder_CannotBeSent()
    {
        var send = () => NewOrder().MarkOrdered(null);

        send.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_AFullyReceivedOrder_IsRefused()
    {
        var order = NewOrder();
        var dolo = Guid.NewGuid();
        order.AddLine(dolo, "Dolo 650", 10m, 14.5m, 12m);
        order.MarkOrdered(null);
        order.RecordReceipt(dolo, 10m, null);

        var cancel = () => order.Cancel(null);

        cancel.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void EstimatedAmount_IsQuantityTimesRateAcrossLines()
    {
        var order = NewOrder();
        order.AddLine(Guid.NewGuid(), "Dolo 650", 10m, 14.5m, 12m);
        order.AddLine(Guid.NewGuid(), "Pan 40", 5m, 110m, 12m);

        order.EstimatedAmount.Should().Be(695m);
    }
}

public class SupplierStatementTests
{
    private static readonly Guid Supplier = Guid.NewGuid();

    private static SupplierEntry Entry(int day, SupplierEntryType type, decimal amount, string no)
        => new(new DateOnly(2026, 10, day), new DateTime(2026, 10, day, 6, 0, 0, DateTimeKind.Utc), type, Guid.NewGuid(), no, null, amount);

    private static readonly IReadOnlyList<SupplierEntry> Entries =
    [
        Entry(1, SupplierEntryType.Purchase, 1000m, "GRN-1"),
        Entry(3, SupplierEntryType.Payment, 400m, "SP-1"),
        Entry(5, SupplierEntryType.Purchase, 600m, "GRN-2"),
        Entry(8, SupplierEntryType.Return, 150m, "PR-1"),
        Entry(12, SupplierEntryType.Payment, 500m, "SP-2"),
    ];

    [Fact]
    public void BuildStatement_KeepsARunningBalance()
    {
        var statement = SupplierAccountService.BuildStatement(Supplier, "Sri Medicals", Entries, null, null);

        statement.OpeningBalance.Should().Be(0m);
        statement.Entries.Select(e => e.Balance).Should().Equal(1000m, 600m, 1200m, 1050m, 550m);
        statement.ClosingBalance.Should().Be(550m);
    }

    [Fact]
    public void BuildStatement_PurchasesAreCreditsEverythingElseIsADebit()
    {
        var statement = SupplierAccountService.BuildStatement(Supplier, "Sri Medicals", Entries, null, null);

        statement.Entries.Sum(e => e.Credit).Should().Be(1600m);
        statement.Entries.Sum(e => e.Debit).Should().Be(1050m);
    }

    [Fact]
    public void BuildStatement_ForADateRange_RollsEarlierEntriesIntoTheOpeningBalance()
    {
        var statement = SupplierAccountService.BuildStatement(Supplier, "Sri Medicals", Entries, new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 10));

        statement.OpeningBalance.Should().Be(600m);
        statement.Entries.Select(e => e.DocumentNo).Should().Equal("GRN-2", "PR-1");
        statement.ClosingBalance.Should().Be(1050m);
    }
}

public class PurchaseReturnServiceTests
{
    private readonly IPharmacyProcurementRepository _repository = Substitute.For<IPharmacyProcurementRepository>();
    private readonly IPharmacyGoodsReceiptRepository _goodsReceiptRepository = Substitute.For<IPharmacyGoodsReceiptRepository>();
    private readonly IPharmacyStoreRepository _storeRepository = Substitute.For<IPharmacyStoreRepository>();
    private readonly FakePharmacyUnitOfWork _unitOfWork = new();
    private readonly StockFixture _stock = new();
    private readonly IProductService _productService = Substitute.For<IProductService>();
    private readonly IProductBatchService _productBatchService = Substitute.For<IProductBatchService>();
    private readonly ISupplierService _supplierService = Substitute.For<ISupplierService>();
    private readonly PharmacyStore _store;
    private readonly PurchaseReturnService _sut;

    private readonly Guid _supplierId = Guid.NewGuid();
    private readonly ProductResponse _product = new() { Id = Guid.NewGuid(), ProductName = "Dolo 650", IsActive = true, UnitsPerPack = 10m, GstRatePercent = 12m };
    private readonly ProductBatchResponse _expiredBatch;

    public PurchaseReturnServiceTests()
    {
        _store = StockFixture.DefaultStore(_storeRepository);
        _expiredBatch = new ProductBatchResponse { Id = Guid.NewGuid(), ProductId = _product.Id, BatchNo = "OLD1", ExpiryDate = PharmacyClock.Today().AddDays(-10), IsActive = true, PurchaseRate = 14.5m };
        _sut = new PurchaseReturnService(_repository, _goodsReceiptRepository, _stock.Balances, _storeRepository, _unitOfWork, _stock.Poster, _productService, _productBatchService, _supplierService);

        _supplierService.GetByIdAsync(_supplierId, Arg.Any<CancellationToken>())
            .Returns(Result<SupplierResponse>.Success(new SupplierResponse { Id = _supplierId, SupplierName = "Sri Medicals", IsActive = true }));
        _productService.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([_product]);
        _productBatchService.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([_expiredBatch]);
        _goodsReceiptRepository.GetLinesForBatchAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);

        _stock.Seed(_store.Id, _product.Id, _expiredBatch.Id, 40m);
    }

    private CreatePurchaseReturnRequest Request(decimal quantity = 30m, decimal? unitRate = null) => new()
    {
        SupplierId = _supplierId,
        Reason = PurchaseReturnReason.Expired,
        Lines = [new PurchaseReturnLineRequest { ProductId = _product.Id, ProductBatchId = _expiredBatch.Id, Quantity = quantity, UnitRate = unitRate }],
    };

    [Fact]
    public async Task CreateAsync_SendsExpiredStockBack_AtTheBatchsOwnCost()
    {
        var result = await _sut.CreateAsync(Request(), null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = result.Value!;
        saved.ReturnNo.Should().Be("PR-2026-000001");

        // 30 tablets at 1.45 = 43.50, plus 12% GST 5.22 = 48.72, settled at 49.
        saved.TaxableAmount.Should().Be(43.50m);
        saved.TaxAmount.Should().Be(5.22m);
        saved.NetAmount.Should().Be(49m);
        _stock.OnHand(_store.Id, _product.Id, _expiredBatch.Id).Should().Be(10m);
    }

    [Fact]
    public async Task CreateAsync_WritesAPurchaseReturnMovementTaggedWithTheDocument()
    {
        var result = await _sut.CreateAsync(Request(), null, CancellationToken.None);

        var row = _stock.Posted.Single();
        row.TransactionType.Should().Be(TransactionType.PurchaseReturn);
        row.SourceType.Should().Be(StockSourceType.PurchaseReturn);
        row.SourceNo.Should().Be(result.Value!.ReturnNo);
    }

    [Fact]
    public async Task CreateAsync_UsesTheRateTheSupplierAgreedWhenGiven()
    {
        var result = await _sut.CreateAsync(Request(quantity: 10m, unitRate: 1m), null, CancellationToken.None);

        result.Value!.TaxableAmount.Should().Be(10m);
    }

    [Fact]
    public async Task CreateAsync_MoreThanTheStoreHolds_IsRefused()
    {
        var result = await _sut.CreateAsync(Request(quantity: 41m), null, CancellationToken.None);

        result.ErrorCode.Should().Be("PHARMACY.INSUFFICIENT_STOCK");
        _unitOfWork.Saves.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_MarksTheReceiptLinesTheBatchCameInOn()
    {
        var receipt = PharmacyGoodsReceipt.Create("GRN-2026-000001", _store.Id, _supplierId, "Sri Medicals", "SM/1", null, PharmacyClock.Today(), false, null, 0m, null, null);
        var line = receipt.AddLine(_product.Id, "Dolo 650", null, _expiredBatch.Id, "OLD1", _expiredBatch.ExpiryDate, 10m, 4m, 0m, 14.5m, 21m, null, 0m, 12m, 58m, 0m, 58m, 6.96m);
        _goodsReceiptRepository.GetLinesForBatchAsync(_expiredBatch.Id, Arg.Any<CancellationToken>()).Returns([line]);

        await _sut.CreateAsync(Request(quantity: 30m), null, CancellationToken.None);

        line.ReturnedUnits.Should().Be(30m);
    }
}

public class GoodsReceiptAgainstOrderTests
{
    [Fact]
    public async Task CreateAsync_AgainstAPurchaseOrder_RecordsWhatArrived()
    {
        var repository = Substitute.For<IPharmacyGoodsReceiptRepository>();
        var storeRepository = Substitute.For<IPharmacyStoreRepository>();
        var procurement = Substitute.For<IPharmacyProcurementRepository>();
        var productService = Substitute.For<IProductService>();
        var batchService = Substitute.For<IProductBatchService>();
        var supplierService = Substitute.For<ISupplierService>();
        var stock = new StockFixture();
        var store = StockFixture.DefaultStore(storeRepository);
        var supplierId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        supplierService.GetByIdAsync(supplierId, Arg.Any<CancellationToken>())
            .Returns(Result<SupplierResponse>.Success(new SupplierResponse { Id = supplierId, SupplierName = "Sri Medicals", IsActive = true }));
        productService.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([new ProductResponse { Id = productId, ProductName = "Dolo 650", IsActive = true, UnitsPerPack = 10m, GstRatePercent = 12m }]);
        batchService.UpsertAsync(productId, Arg.Any<UpsertProductBatchRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<ProductBatchResponse>.Success(new ProductBatchResponse { Id = Guid.NewGuid(), ProductId = productId, BatchNo = "DL1", ExpiryDate = call.Arg<UpsertProductBatchRequest>().ExpiryDate, IsActive = true }));

        var order = PharmacyPurchaseOrder.Create("PO-2026-000001", store.Id, supplierId, "Sri Medicals", PharmacyClock.Today(), null, null, null);
        order.AddLine(productId, "Dolo 650", 10m, 14.5m, 12m);
        order.MarkOrdered(null);
        procurement.GetOrderAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var sut = new GoodsReceiptService(repository, storeRepository, new FakePharmacyUnitOfWork(), stock.Poster, productService, batchService, supplierService, procurement);

        var result = await sut.CreateAsync(
            new CreateGoodsReceiptRequest
            {
                SupplierId = supplierId,
                SupplierInvoiceNo = "SM/2001",
                PurchaseOrderId = order.Id,
                Lines = [new GoodsReceiptLineRequest { ProductId = productId, BatchNo = "DL1", ExpiryDate = PharmacyClock.Today().AddYears(1), Quantity = 6m, FreeQuantity = 1m, PurchaseRate = 14.5m, Mrp = 21m }],
            },
            null,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.PurchaseOrderId.Should().Be(order.Id);

        // Six billed strips count against the order; the free strip is a bonus, not a delivery.
        order.Lines.Single().ReceivedQuantity.Should().Be(6m);
        order.Status.Should().Be(PurchaseOrderStatus.PartlyReceived);
    }
}
