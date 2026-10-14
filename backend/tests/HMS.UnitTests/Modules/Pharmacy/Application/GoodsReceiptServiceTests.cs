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

public class GoodsReceiptServiceTests
{
    private readonly IPharmacyGoodsReceiptRepository _repository = Substitute.For<IPharmacyGoodsReceiptRepository>();
    private readonly IPharmacyStoreRepository _storeRepository = Substitute.For<IPharmacyStoreRepository>();
    private readonly FakePharmacyUnitOfWork _unitOfWork = new();
    private readonly StockFixture _stock = new();
    private readonly IProductService _productService = Substitute.For<IProductService>();
    private readonly IProductBatchService _productBatchService = Substitute.For<IProductBatchService>();
    private readonly ISupplierService _supplierService = Substitute.For<ISupplierService>();
    private readonly PharmacyStore _store;
    private readonly GoodsReceiptService _sut;

    private readonly Guid _supplierId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _batchId = Guid.NewGuid();
    private static readonly DateOnly Expiry = PharmacyClock.Today().AddYears(2);

    public GoodsReceiptServiceTests()
    {
        _store = StockFixture.DefaultStore(_storeRepository);
        _sut = new GoodsReceiptService(_repository, _storeRepository, _unitOfWork, _stock.Poster, _productService, _productBatchService, _supplierService);

        _supplierService.GetByIdAsync(_supplierId, Arg.Any<CancellationToken>())
            .Returns(Result<SupplierResponse>.Success(new SupplierResponse { Id = _supplierId, SupplierName = "Sri Medicals", IsActive = true }));

        // A strip of 10 at 12% GST.
        _productService.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([new ProductResponse { Id = _productId, ProductName = "Dolo 650", IsActive = true, UnitsPerPack = 10m, GstRatePercent = 12m, HsnCode = "3004" }]);

        _productBatchService.UpsertAsync(_productId, Arg.Any<UpsertProductBatchRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<ProductBatchResponse>.Success(new ProductBatchResponse
            {
                Id = _batchId,
                ProductId = _productId,
                BatchNo = call.Arg<UpsertProductBatchRequest>().BatchNo,
                ExpiryDate = call.Arg<UpsertProductBatchRequest>().ExpiryDate,
                IsActive = true,
            }));
    }

    private CreateGoodsReceiptRequest NewRequest(params GoodsReceiptLineRequest[] lines) => new()
    {
        SupplierId = _supplierId,
        SupplierInvoiceNo = "SM/1042",
        Lines = lines.Length > 0 ? lines : [Line()],
    };

    private GoodsReceiptLineRequest Line(decimal quantity = 10m, decimal free = 1m, decimal rate = 14.50m, decimal discount = 5m) => new()
    {
        ProductId = _productId,
        BatchNo = "DL2406",
        ExpiryDate = Expiry,
        Quantity = quantity,
        FreeQuantity = free,
        PurchaseRate = rate,
        Mrp = 21m,
        DiscountPercent = discount,
    };

    [Fact]
    public async Task CreateAsync_ReceivesBilledAndFreePacksAsUnits()
    {
        var result = await _sut.CreateAsync(NewRequest(), null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReceiptNo.Should().Be("GRN-2026-000001");

        // 10 billed + 1 free strips of 10 tablets.
        _stock.OnHand(_store.Id, _productId, _batchId).Should().Be(110m);
        result.Value.Lines.Single().ReceivedUnits.Should().Be(110m);
    }

    [Fact]
    public async Task CreateAsync_WorksOutTheInvoiceTheWayTheSupplierDid()
    {
        var result = await _sut.CreateAsync(NewRequest(), null, CancellationToken.None);

        var receipt = result.Value!;
        receipt.GrossAmount.Should().Be(145m);
        receipt.DiscountAmount.Should().Be(7.25m);
        receipt.TaxableAmount.Should().Be(137.75m);
        receipt.TaxAmount.Should().Be(16.53m);
        receipt.NetAmount.Should().Be(154m);
        receipt.RoundOff.Should().Be(-0.28m);
    }

    [Fact]
    public async Task CreateAsync_SpreadsCostOverFreeGoods_AndTagsTheLedgerWithTheReceipt()
    {
        var result = await _sut.CreateAsync(NewRequest(), null, CancellationToken.None);

        var row = _stock.Posted.Single();
        row.TransactionType.Should().Be(TransactionType.Receipt);
        row.SourceType.Should().Be(StockSourceType.GoodsReceipt);
        row.SourceId.Should().Be(result.Value!.Id);
        row.SourceNo.Should().Be("GRN-2026-000001");

        // 137.75 taxable over 110 tablets — the free strip makes every tablet cheaper.
        row.UnitCost.Should().Be(1.2523m);
    }

    [Fact]
    public async Task CreateAsync_PutsTheInvoicePricesOnTheBatch()
    {
        await _sut.CreateAsync(NewRequest(), null, CancellationToken.None);

        await _productBatchService.Received(1).UpsertAsync(
            _productId,
            Arg.Is<UpsertProductBatchRequest>(b => b.BatchNo == "DL2406" && b.Mrp == 21m && b.PurchaseRate == 14.50m && b.ExpiryDate == Expiry),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_UsesTheLinesOwnGstRateWhenGiven()
    {
        var result = await _sut.CreateAsync(NewRequest(Line(discount: 0m) with { GstRatePercent = 5m }), null, CancellationToken.None);

        result.Value!.TaxAmount.Should().Be(7.25m);
        result.Value.Lines.Single().GstRatePercent.Should().Be(5m);
    }

    [Fact]
    public async Task CreateAsync_TheSameSupplierInvoiceTwice_IsRefused()
    {
        _repository.ExistsForSupplierInvoiceAsync(_supplierId, "SM/1042", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(NewRequest(), null, CancellationToken.None);

        result.ErrorCode.Should().Be("PHARMACY.DUPLICATE_SUPPLIER_INVOICE");
        _stock.Posted.Should().BeEmpty();
        _unitOfWork.Saves.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_AnAlreadyExpiredBatch_IsRefusedBeforeAnythingIsSaved()
    {
        var expired = Line() with { ExpiryDate = PharmacyClock.Today().AddDays(-1) };

        var result = await _sut.CreateAsync(NewRequest(expired), null, CancellationToken.None);

        result.ErrorCode.Should().Be("PHARMACY.BATCH_EXPIRED");
        await _productBatchService.DidNotReceive().UpsertAsync(Arg.Any<Guid>(), Arg.Any<UpsertProductBatchRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_AnInactiveSupplier_IsRefused()
    {
        _supplierService.GetByIdAsync(_supplierId, Arg.Any<CancellationToken>())
            .Returns(Result<SupplierResponse>.Success(new SupplierResponse { Id = _supplierId, SupplierName = "Sri Medicals", IsActive = false }));

        var result = await _sut.CreateAsync(NewRequest(), null, CancellationToken.None);

        result.ErrorCode.Should().Be("PHARMACY.INVALID_SUPPLIER");
    }

    [Fact]
    public async Task CreateAsync_AnUnknownProductOnALine_NamesTheLine()
    {
        var stranger = Line() with { ProductId = Guid.NewGuid() };

        var result = await _sut.CreateAsync(NewRequest(Line(), stranger), null, CancellationToken.None);

        result.ErrorCode.Should().Be("PHARMACY.INVALID_PRODUCT");
        result.Error.Should().StartWith("Line 2:");
    }
}

public class ProductImportServiceRulesTests
{
    [Fact]
    public void Check_ARowWithJustAName_IsFine()
    {
        ProductImportService.Check(new ImportProductRow { ProductName = "Dolo 650" }).Should().BeNull();
    }

    [Fact]
    public void Check_OpeningStockWithoutABatchOrExpiry_IsRefused()
    {
        ProductImportService.Check(new ImportProductRow { ProductName = "Dolo 650", OpeningQuantity = 30m })
            .Should().Be("Opening stock needs a batch number.");
        ProductImportService.Check(new ImportProductRow { ProductName = "Dolo 650", OpeningQuantity = 30m, BatchNo = "DL1" })
            .Should().Be("Opening stock needs an expiry date.");
    }

    [Fact]
    public void Check_ExpiredOpeningStock_IsNotPutOnTheShelf()
    {
        var row = new ImportProductRow { ProductName = "Dolo 650", OpeningQuantity = 30m, BatchNo = "DL1", ExpiryDate = PharmacyClock.Today().AddDays(-1) };

        ProductImportService.Check(row).Should().Contain("already expired");
    }

    [Fact]
    public void Check_SellingRateAboveMrp_IsRefused()
    {
        ProductImportService.Check(new ImportProductRow { ProductName = "Dolo 650", Mrp = 21m, SellingRate = 22m }).Should().Be("Selling rate is above MRP.");
    }

    [Theory]
    [InlineData("H1", DrugSchedule.H1)]
    [InlineData("Schedule H", DrugSchedule.H)]
    [InlineData("Sch. X", DrugSchedule.X)]
    [InlineData("NDPS", DrugSchedule.Narcotic)]
    [InlineData("OTC", DrugSchedule.None)]
    public void ParseSchedule_ReadsTheWaysSpreadsheetsWriteIt(string text, DrugSchedule expected)
    {
        ProductImportService.ParseSchedule(text).Should().Be(expected);
    }

    [Fact]
    public void ParseSchedule_SomethingItDoesNotKnow_IsNull()
    {
        ProductImportService.ParseSchedule("Schedule Q").Should().BeNull();
        ProductImportService.ParseSchedule(null).Should().BeNull();
    }

    [Fact]
    public void Code_IsStableFitsTheColumnAndTellsSimilarNamesApart()
    {
        var first = ProductImportService.Code("Sun Pharmaceutical Industries Ltd", 20);
        var again = ProductImportService.Code("  sun pharmaceutical industries ltd ", 20);
        var other = ProductImportService.Code("Sun Pharmaceutical Industries Ltd (Export)", 20);

        first.Should().Be(again);
        first.Should().NotBe(other);
        first.Length.Should().BeLessThanOrEqualTo(20);
        other.Length.Should().BeLessThanOrEqualTo(20);
    }
}
