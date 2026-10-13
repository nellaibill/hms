using FluentAssertions;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Contracts;
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

/// <summary>Builds the stock picture SalePlanner works from, without a database.</summary>
internal static class Shelf
{
    public static readonly DateOnly Soon = PharmacyClock.Today().AddMonths(3);
    public static readonly DateOnly Later = PharmacyClock.Today().AddYears(2);

    /// <summary>A strip-of-10 product at MRP 21 (2.10 a tablet), 12% GST.</summary>
    public static ProductResponse Product(string name = "Dolo 650", DrugSchedule schedule = DrugSchedule.None, decimal gst = 12m) => new()
    {
        Id = Guid.NewGuid(),
        ProductName = name,
        IsActive = true,
        UnitsPerPack = 10m,
        Mrp = 21m,
        SellingPrice = 21m,
        GstRatePercent = gst,
        Schedule = schedule,
        HsnCode = "3004",
    };

    public static SellableBatch Batch(ProductResponse product, string batchNo, DateOnly expiry, decimal quantity, decimal packMrp = 21m, decimal? packCost = 14.5m)
    {
        var batch = new ProductBatchResponse { Id = Guid.NewGuid(), ProductId = product.Id, BatchNo = batchNo, ExpiryDate = expiry, IsActive = true, Mrp = packMrp, PurchaseRate = packCost };
        return new SellableBatch(batch, quantity, PharmacyPricing.UnitPrice(product, batch), PharmacyPricing.UnitMrp(product, batch), PharmacyPricing.UnitCost(product, batch));
    }

    public static IReadOnlyDictionary<Guid, SellableItem> Of(ProductResponse product, params SellableBatch[] batches)
        => new Dictionary<Guid, SellableItem> { [product.Id] = new SellableItem(product, batches) };
}

public class SalePlannerTests
{
    [Fact]
    public void PlanLines_WithNoBatchNamed_TakesTheEarliestExpiryFirst()
    {
        var product = Shelf.Product();
        var stock = Shelf.Of(product, Shelf.Batch(product, "OLD", Shelf.Soon, 30m), Shelf.Batch(product, "NEW", Shelf.Later, 100m));

        var result = SalePlanner.PlanLines([new SaleLineRequest { ProductId = product.Id, Quantity = 10m }], stock, 0m);

        result.Value!.Should().ContainSingle().Which.Batch.BatchNo.Should().Be("OLD");
    }

    [Fact]
    public void PlanLines_WhenTheFirstBatchRunsOut_SplitsAcrossBatches()
    {
        var product = Shelf.Product();
        var stock = Shelf.Of(product, Shelf.Batch(product, "OLD", Shelf.Soon, 6m), Shelf.Batch(product, "NEW", Shelf.Later, 100m));

        var result = SalePlanner.PlanLines([new SaleLineRequest { ProductId = product.Id, Quantity = 10m }], stock, 0m);

        result.Value!.Select(l => (l.Batch.BatchNo, l.Quantity)).Should().Equal(("OLD", 6m), ("NEW", 4m));
    }

    [Fact]
    public void PlanLines_PricesEachBatchAtItsOwnMrp()
    {
        var product = Shelf.Product();
        var stock = Shelf.Of(product, Shelf.Batch(product, "OLD", Shelf.Soon, 5m, packMrp: 21m), Shelf.Batch(product, "NEW", Shelf.Later, 100m, packMrp: 23m));

        var lines = SalePlanner.PlanLines([new SaleLineRequest { ProductId = product.Id, Quantity = 10m }], stock, 0m).Value!;

        lines[0].UnitPrice.Should().Be(2.1m);
        lines[0].Amount.Total.Should().Be(10.50m);
        lines[1].UnitPrice.Should().Be(2.3m);
        lines[1].Amount.Total.Should().Be(11.50m);
    }

    [Fact]
    public void PlanLines_FindsTheGstInsideTheMrp()
    {
        var product = Shelf.Product();
        var stock = Shelf.Of(product, Shelf.Batch(product, "B1", Shelf.Later, 100m));

        // 40 tablets at 2.10 = 84.00 including 12% GST: 75.00 + 9.00.
        var line = SalePlanner.PlanLines([new SaleLineRequest { ProductId = product.Id, Quantity = 40m }], stock, 0m).Value!.Single();

        line.Amount.Total.Should().Be(84m);
        line.Amount.Taxable.Should().Be(75m);
        line.Amount.Tax.Should().Be(9m);
    }

    [Fact]
    public void PlanLines_ALinesOwnDiscountWinsOverTheBills()
    {
        var product = Shelf.Product();
        var stock = Shelf.Of(product, Shelf.Batch(product, "B1", Shelf.Later, 100m));

        var lines = SalePlanner.PlanLines(
            [
                new SaleLineRequest { ProductId = product.Id, Quantity = 10m },
                new SaleLineRequest { ProductId = product.Id, Quantity = 10m, DiscountPercent = 0m },
            ],
            stock,
            billDiscountPercent: 10m).Value!;

        lines[0].Amount.Discount.Should().Be(2.10m);
        lines[0].Amount.Total.Should().Be(18.90m);
        lines[1].Amount.Discount.Should().Be(0m);
        lines[1].Amount.Total.Should().Be(21m);
    }

    [Fact]
    public void PlanLines_StockPromisedToAnEarlierLine_IsNotOfferedAgain()
    {
        var product = Shelf.Product();
        var stock = Shelf.Of(product, Shelf.Batch(product, "B1", Shelf.Later, 10m));

        var result = SalePlanner.PlanLines(
            [new SaleLineRequest { ProductId = product.Id, Quantity = 6m }, new SaleLineRequest { ProductId = product.Id, Quantity = 6m }],
            stock,
            0m);

        result.ErrorCode.Should().Be("PHARMACY.INSUFFICIENT_STOCK");
        result.Error.Should().Be("Line 2: only 4 of Dolo 650 available — 6 needed.");
    }

    [Fact]
    public void PlanLines_ANamedBatchWithNoStock_IsRefused()
    {
        var product = Shelf.Product();
        var stock = Shelf.Of(product, Shelf.Batch(product, "B1", Shelf.Later, 10m));

        var result = SalePlanner.PlanLines([new SaleLineRequest { ProductId = product.Id, ProductBatchId = Guid.NewGuid(), Quantity = 1m }], stock, 0m);

        result.ErrorCode.Should().Be("PHARMACY.INVALID_BATCH");
    }

    [Fact]
    public void SettlePayments_CashOverTheBill_IsChangeAndIsNotRecorded()
    {
        var result = SalePlanner.SettlePayments(316m, [new SalePaymentRequest { Method = SalePaymentMethod.Cash, Amount = 500m }]);

        var (payments, change, due) = result.Value;
        payments.Should().ContainSingle().Which.Amount.Should().Be(316m);
        change.Should().Be(184m);
        due.Should().Be(0m);
    }

    [Fact]
    public void SettlePayments_PartUpiPartCash_ChangeComesOutOfTheCash()
    {
        var result = SalePlanner.SettlePayments(
            316m,
            [new SalePaymentRequest { Method = SalePaymentMethod.Upi, Amount = 200m, ReferenceNo = "UPI123" }, new SalePaymentRequest { Method = SalePaymentMethod.Cash, Amount = 200m }]);

        var (payments, change, due) = result.Value;
        payments.Should().BeEquivalentTo([new PlannedPayment(SalePaymentMethod.Upi, 200m, "UPI123"), new PlannedPayment(SalePaymentMethod.Cash, 116m, null)]);
        change.Should().Be(84m);
        due.Should().Be(0m);
    }

    [Fact]
    public void SettlePayments_CardForMoreThanTheBill_IsRefused()
    {
        var result = SalePlanner.SettlePayments(316m, [new SalePaymentRequest { Method = SalePaymentMethod.Card, Amount = 400m }]);

        result.ErrorCode.Should().Be("PHARMACY.INVALID_PAYMENT");
    }

    [Fact]
    public void SettlePayments_LessThanTheBill_LeavesABalanceDue()
    {
        var (payments, change, due) = SalePlanner.SettlePayments(316m, [new SalePaymentRequest { Method = SalePaymentMethod.Cash, Amount = 100m }]).Value;

        payments.Sum(p => p.Amount).Should().Be(100m);
        change.Should().Be(0m);
        due.Should().Be(216m);
    }
}

public class SaleServiceTests
{
    private readonly IPharmacySaleRepository _repository = Substitute.For<IPharmacySaleRepository>();
    private readonly IPharmacyStoreRepository _storeRepository = Substitute.For<IPharmacyStoreRepository>();
    private readonly FakePharmacyUnitOfWork _unitOfWork = new();
    private readonly StockFixture _stock = new();
    private readonly ISellableStockReader _stockReader = Substitute.For<ISellableStockReader>();
    private readonly IProductService _productService = Substitute.For<IProductService>();
    private readonly IPatientService _patientService = Substitute.For<IPatientService>();
    private readonly IAdmissionChargePoster _admissionCharges = Substitute.For<IAdmissionChargePoster>();
    private readonly IPharmacySettingsRepository _settings = Substitute.For<IPharmacySettingsRepository>();
    private readonly PharmacyStore _store;
    private readonly SaleService _sut;

    private readonly ProductResponse _product = Shelf.Product();
    private readonly SellableBatch _batch;
    private readonly Guid _patientId = Guid.NewGuid();
    private readonly Guid _admissionId = Guid.NewGuid();
    private PharmacySale? _saved;

    public SaleServiceTests()
    {
        _store = StockFixture.DefaultStore(_storeRepository);
        _batch = Shelf.Batch(_product, "DL2406", Shelf.Later, 100m);
        _sut = new SaleService(_repository, _storeRepository, _unitOfWork, _stock.Poster, _stockReader, _productService, _patientService, _admissionCharges, _settings);

        _settings.GetAsync(Arg.Any<CancellationToken>()).Returns((PharmacySettings?)null);

        Stock(_product, _batch);
        _stock.Seed(_store.Id, _product.Id, _batch.Batch.Id, 100m);

        _patientService.GetByIdAsync(_patientId, Arg.Any<CancellationToken>())
            .Returns(Result<PatientResponse>.Success(new PatientResponse { Id = _patientId, FirstName = "Kala", LastName = "R", Uhid = "P-2026-000412", PrimaryPhone = "9000000002" }));

        // NSubstitute's default for a Task<string?> is an empty string, which would read as
        // "there is a problem with this admission".
        _admissionCharges.ValidateAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((string?)null);

        _repository.When(r => r.AddAsync(Arg.Any<PharmacySale>(), Arg.Any<CancellationToken>())).Do(call => _saved = call.Arg<PharmacySale>());
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(_ => _saved);
    }

    private void Stock(ProductResponse product, params SellableBatch[] batches)
    {
        _productService.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([product]);
        _stockReader.ReadAsync(Arg.Any<IReadOnlyCollection<ProductResponse>>(), _store.Id, Arg.Any<CancellationToken>()).Returns(Shelf.Of(product, batches));
    }

    private CreateSaleRequest Cart(decimal quantity = 40m, decimal cash = 100m) => new()
    {
        Lines = [new SaleLineRequest { ProductId = _product.Id, Quantity = quantity }],
        Payments = cash > 0 ? [new SalePaymentRequest { Method = SalePaymentMethod.Cash, Amount = cash }] : [],
    };

    [Fact]
    public async Task CreateAsync_AWalkInCashSale_MakesANumberedBillAndGivesChange()
    {
        var result = await _sut.CreateAsync(Cart(), null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var bill = result.Value!;
        bill.BillNo.Should().Be("PH-2026-000001");
        bill.CustomerName.Should().Be("Cash customer");
        bill.NetAmount.Should().Be(84m);
        bill.TaxableAmount.Should().Be(75m);
        bill.TaxAmount.Should().Be(9m);
        bill.PaidAmount.Should().Be(84m);
        bill.ChangeGiven.Should().Be(16m);
        bill.BalanceDue.Should().Be(0m);
    }

    [Fact]
    public async Task CreateAsync_TakesTheStockAndWritesTheLedgerFromTheBill()
    {
        var result = await _sut.CreateAsync(Cart(), null, CancellationToken.None);

        _stock.OnHand(_store.Id, _product.Id, _batch.Batch.Id).Should().Be(60m);
        var row = _stock.Posted.Single();
        row.TransactionType.Should().Be(TransactionType.Sale);
        row.SourceType.Should().Be(StockSourceType.Sale);
        row.SourceNo.Should().Be(result.Value!.BillNo);
        row.UnitPrice.Should().Be(2.1m);
        row.UnitCost.Should().Be(1.45m);
    }

    [Fact]
    public async Task CreateAsync_RoundsTheBillToTheRupee()
    {
        // 7 tablets at 2.10 = 14.70 → 15, with 0.30 round-off.
        var result = await _sut.CreateAsync(Cart(quantity: 7m, cash: 15m), null, CancellationToken.None);

        result.Value!.NetAmount.Should().Be(15m);
        result.Value.RoundOff.Should().Be(0.30m);
    }

    [Fact]
    public async Task CreateAsync_ForARegisteredPatient_PutsTheirNameAndUhidOnTheBill()
    {
        var result = await _sut.CreateAsync(Cart() with { PatientId = _patientId }, null, CancellationToken.None);

        result.Value!.CustomerName.Should().Be("Kala R");
        result.Value.PatientUhid.Should().Be("P-2026-000412");
        result.Value.CustomerPhone.Should().Be("9000000002");
    }

    [Fact]
    public async Task CreateAsync_PaymentShort_IsRefusedAndNothingMoves()
    {
        var result = await _sut.CreateAsync(Cart(cash: 50m), null, CancellationToken.None);

        result.ErrorCode.Should().Be("PHARMACY.PAYMENT_SHORT");
        _stock.Posted.Should().BeEmpty();
        _unitOfWork.Saves.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_ADiscountAboveTheCountersLimit_IsRefused()
    {
        var settings = PharmacySettings.Create(null);
        settings.Update(null, null, null, null, null, null, BillPrintFormat.A5, maxDiscountPercent: 10m, expiryAlertDays: 90, null);
        _settings.GetAsync(Arg.Any<CancellationToken>()).Returns(settings);

        var result = await _sut.CreateAsync(Cart() with { DiscountPercent = 15m }, null, CancellationToken.None);

        result.ErrorCode.Should().Be("PHARMACY.DISCOUNT_LIMIT");
        _stock.Posted.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_CreditForAWalkIn_IsRefused()
    {
        var result = await _sut.CreateAsync(Cart(cash: 0m) with { AllowCredit = true }, null, CancellationToken.None);

        result.ErrorCode.Should().Be("PHARMACY.CUSTOMER_REQUIRED");
    }

    [Fact]
    public async Task CreateAsync_CreditForARegisteredPatient_LeavesABalanceDue()
    {
        var result = await _sut.CreateAsync(Cart(cash: 50m) with { PatientId = _patientId, AllowCredit = true }, null, CancellationToken.None);

        result.Value!.PaidAmount.Should().Be(50m);
        result.Value.BalanceDue.Should().Be(34m);
    }

    [Fact]
    public async Task CreateAsync_AScheduleH1DrugWithoutADoctor_IsRefused()
    {
        var alprax = Shelf.Product("Alprax 0.25", DrugSchedule.H1);
        Stock(alprax, Shelf.Batch(alprax, "AX1", Shelf.Later, 50m));

        var request = new CreateSaleRequest
        {
            CustomerName = "Kala R",
            Lines = [new SaleLineRequest { ProductId = alprax.Id, Quantity = 10m }],
            Payments = [new SalePaymentRequest { Method = SalePaymentMethod.Cash, Amount = 100m }],
        };

        var result = await _sut.CreateAsync(request, null, CancellationToken.None);

        result.ErrorCode.Should().Be("PHARMACY.PRESCRIPTION_DETAILS_REQUIRED");
        result.Error.Should().Contain("Alprax 0.25").And.Contain("Schedule H1");
    }

    [Fact]
    public async Task CreateAsync_AScheduleH1DrugWithPatientAndDoctor_IsSold()
    {
        var alprax = Shelf.Product("Alprax 0.25", DrugSchedule.H1);
        var batch = Shelf.Batch(alprax, "AX1", Shelf.Later, 50m);
        Stock(alprax, batch);
        _stock.Seed(_store.Id, alprax.Id, batch.Batch.Id, 50m);

        var request = new CreateSaleRequest
        {
            CustomerName = "Kala R",
            DoctorName = "Dr. S. Karthikeyan",
            Lines = [new SaleLineRequest { ProductId = alprax.Id, Quantity = 10m }],
            Payments = [new SalePaymentRequest { Method = SalePaymentMethod.Cash, Amount = 100m }],
        };

        var result = await _sut.CreateAsync(request, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines.Single().Schedule.Should().Be(DrugSchedule.H1);
        result.Value.DoctorName.Should().Be("Dr. S. Karthikeyan");
    }

    [Fact]
    public async Task CreateAsync_ChargedToAnAdmission_PostsTheUnpaidAmountToIt()
    {
        var request = Cart(cash: 0m) with { PatientId = _patientId, AdmissionId = _admissionId, ChargeToAdmission = true };

        var result = await _sut.CreateAsync(request, null, CancellationToken.None);

        result.Value!.ChargedToAdmission.Should().BeTrue();
        result.Value.BalanceDue.Should().Be(0m, "the admission owes it, not the counter");
        await _admissionCharges.Received(1).PostSaleAsync(_admissionId, "PH-2026-000001", 84m, Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_AnAdmissionThatCannotBeCharged_IsRefused()
    {
        _admissionCharges.ValidateAsync(_admissionId, _patientId, Arg.Any<CancellationToken>()).Returns("Admission IP-1 is not currently admitted, so nothing more can be charged to it.");

        var result = await _sut.CreateAsync(Cart(cash: 0m) with { PatientId = _patientId, AdmissionId = _admissionId, ChargeToAdmission = true }, null, CancellationToken.None);

        result.ErrorCode.Should().Be("PHARMACY.INVALID_ADMISSION");
        _stock.Posted.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateReturnAsync_PutsTheGoodsBackAndRefundsWhatWasPaidForThem()
    {
        var bill = (await _sut.CreateAsync(Cart(), null, CancellationToken.None)).Value!;

        var result = await _sut.CreateReturnAsync(
            bill.Id,
            new CreateSaleReturnRequest { Lines = [new SaleReturnLineRequest { SaleLineId = bill.Lines[0].Id, Quantity = 10m }], Reason = "Doctor changed the medicine" },
            null,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReturnNo.Should().Be("SR-2026-000002");
        result.Value.ReturnAmount.Should().Be(21m);
        result.Value.RefundAmount.Should().Be(21m);
        _stock.OnHand(_store.Id, _product.Id, _batch.Batch.Id).Should().Be(70m);
        _stock.Posted.Last().TransactionType.Should().Be(TransactionType.SaleReturn);
    }

    [Fact]
    public async Task CreateReturnAsync_GoodsNotFitToSell_AreCreditedButNotRestocked()
    {
        var bill = (await _sut.CreateAsync(Cart(), null, CancellationToken.None)).Value!;

        var result = await _sut.CreateReturnAsync(
            bill.Id,
            new CreateSaleReturnRequest { Lines = [new SaleReturnLineRequest { SaleLineId = bill.Lines[0].Id, Quantity = 10m }], Restock = false },
            null,
            CancellationToken.None);

        result.Value!.RefundAmount.Should().Be(21m);
        result.Value.Restocked.Should().BeFalse();
        _stock.OnHand(_store.Id, _product.Id, _batch.Batch.Id).Should().Be(60m);
    }

    [Fact]
    public async Task CreateReturnAsync_MoreThanWasSold_IsRefused()
    {
        var bill = (await _sut.CreateAsync(Cart(), null, CancellationToken.None)).Value!;

        var result = await _sut.CreateReturnAsync(
            bill.Id,
            new CreateSaleReturnRequest { Lines = [new SaleReturnLineRequest { SaleLineId = bill.Lines[0].Id, Quantity = 41m }] },
            null,
            CancellationToken.None);

        result.ErrorCode.Should().Be("PHARMACY.INVALID_QUANTITY");
    }

    [Fact]
    public async Task CreateReturnAsync_OnABillStillOwed_ComesOffTheDueBeforeAnyRefund()
    {
        var bill = (await _sut.CreateAsync(Cart(cash: 50m) with { PatientId = _patientId, AllowCredit = true }, null, CancellationToken.None)).Value!;

        // 84 billed, 50 paid, 34 due. 20 tablets (42) come back: 34 clears the due, 8 is refunded.
        var result = await _sut.CreateReturnAsync(
            bill.Id,
            new CreateSaleReturnRequest { Lines = [new SaleReturnLineRequest { SaleLineId = bill.Lines[0].Id, Quantity = 20m }] },
            null,
            CancellationToken.None);

        result.Value!.ReturnAmount.Should().Be(42m);
        result.Value.RefundAmount.Should().Be(8m);
        _saved!.BalanceDue.Should().Be(0m);
    }

    [Fact]
    public async Task CreateReturnAsync_OnABillChargedToAnAdmission_TakesItOffTheAdmission()
    {
        var bill = (await _sut.CreateAsync(Cart(cash: 0m) with { PatientId = _patientId, AdmissionId = _admissionId, ChargeToAdmission = true }, null, CancellationToken.None)).Value!;

        var result = await _sut.CreateReturnAsync(
            bill.Id,
            new CreateSaleReturnRequest { Lines = [new SaleReturnLineRequest { SaleLineId = bill.Lines[0].Id, Quantity = 10m }] },
            null,
            CancellationToken.None);

        result.Value!.RefundAmount.Should().Be(0m, "nothing was paid at the counter");
        await _admissionCharges.Received(1).PostSaleReturnAsync(_admissionId, result.Value.ReturnNo, bill.BillNo, 21m, Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CollectPaymentAsync_TakesMoneyAgainstTheBalanceDue()
    {
        var bill = (await _sut.CreateAsync(Cart(cash: 50m) with { PatientId = _patientId, AllowCredit = true }, null, CancellationToken.None)).Value!;

        var result = await _sut.CollectPaymentAsync(bill.Id, new CollectSalePaymentRequest { Method = SalePaymentMethod.Upi, Amount = 34m, ReferenceNo = "UPI9" }, null, CancellationToken.None);

        result.Value!.BalanceDue.Should().Be(0m);
        result.Value.Payments.Should().HaveCount(2);
    }

    [Fact]
    public async Task CollectPaymentAsync_MoreThanIsDue_IsRefused()
    {
        var bill = (await _sut.CreateAsync(Cart(cash: 50m) with { PatientId = _patientId, AllowCredit = true }, null, CancellationToken.None)).Value!;

        var result = await _sut.CollectPaymentAsync(bill.Id, new CollectSalePaymentRequest { Method = SalePaymentMethod.Cash, Amount = 35m }, null, CancellationToken.None);

        result.ErrorCode.Should().Be("PHARMACY.INVALID_PAYMENT");
    }
}
