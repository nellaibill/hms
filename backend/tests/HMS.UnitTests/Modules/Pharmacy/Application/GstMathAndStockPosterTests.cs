using FluentAssertions;
using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.Pharmacy.Application;

public class GstMathTests
{
    [Fact]
    public void Exclusive_TakesDiscountOffFirstThenAddsTax()
    {
        // 10 strips at 14.50, 5% trade discount, 12% GST.
        var amount = GstMath.Exclusive(145m, 5m, 12m);

        amount.Gross.Should().Be(145m);
        amount.Discount.Should().Be(7.25m);
        amount.Taxable.Should().Be(137.75m);
        amount.Tax.Should().Be(16.53m);
        amount.Total.Should().Be(154.28m);
    }

    [Fact]
    public void Inclusive_FindsTheTaxInsideThePriceTheCustomerPays()
    {
        // MRP 112 including 12% GST: 100 is the medicine, 12 is tax.
        var amount = GstMath.Inclusive(112m, 0m, 12m);

        amount.Taxable.Should().Be(100m);
        amount.Tax.Should().Be(12m);
        amount.Total.Should().Be(112m);
    }

    [Fact]
    public void Inclusive_TaxAndTaxableAlwaysAddBackToWhatWasCharged()
    {
        var amount = GstMath.Inclusive(8.40m, 0.84m, 5m);

        amount.Total.Should().Be(7.56m);
        (amount.Taxable + amount.Tax).Should().Be(amount.Total);
    }

    [Fact]
    public void Inclusive_NeverDiscountsBelowZero()
    {
        var amount = GstMath.Inclusive(50m, 80m, 12m);

        amount.Discount.Should().Be(50m);
        amount.Total.Should().Be(0m);
        amount.Tax.Should().Be(0m);
    }

    [Theory]
    [InlineData(154.28, -0.28, 154)]
    [InlineData(154.50, 0.50, 155)]
    [InlineData(154.00, 0.00, 154)]
    public void RoundToRupee_ShowsThePaiseAsRoundOff(decimal total, decimal roundOff, decimal net)
    {
        GstMath.RoundToRupee(total).Should().Be((roundOff, net));
    }

    [Theory]
    [InlineData(12.00, 6.00, 6.00)]
    [InlineData(16.53, 8.27, 8.26)]
    public void SplitIntraState_HalvesAndKeepsTheOddPaisa(decimal tax, decimal cgst, decimal sgst)
    {
        GstMath.SplitIntraState(tax).Should().Be((cgst, sgst));
    }
}

public class StockPosterTests
{
    private readonly StockFixture _stock = new();
    private readonly Guid _store = Guid.NewGuid();
    private readonly Guid _product = Guid.NewGuid();
    private readonly Guid _batch = Guid.NewGuid();

    [Fact]
    public async Task Inward_OnABatchTheStoreHasNeverHeld_CreatesItsBalance()
    {
        var result = await _stock.Poster.PostAsync(new StockMovement(TransactionType.Receipt, _store, _product, _batch, 30m), null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BalanceAfter.Should().Be(30m);
        _stock.OnHand(_store, _product, _batch).Should().Be(30m);
    }

    [Fact]
    public async Task Outward_BeyondWhatIsHeld_IsRefusedAndMovesNothing()
    {
        _stock.Seed(_store, _product, _batch, 5m);

        var result = await _stock.Poster.PostAsync(
            new StockMovement(TransactionType.Sale, _store, _product, _batch, 6m, Label: "Dolo 650 (DL2406)"), null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PHARMACY.INSUFFICIENT_STOCK");
        result.Error.Should().Contain("Only 5 of Dolo 650 (DL2406)");
        _stock.OnHand(_store, _product, _batch).Should().Be(5m);
        _stock.Posted.Should().BeEmpty();
    }

    [Fact]
    public async Task TwoLinesOnTheSameBatch_TheSecondSeesTheFirst()
    {
        _stock.Seed(_store, _product, _batch, 10m);

        var first = await _stock.Poster.PostAsync(new StockMovement(TransactionType.Sale, _store, _product, _batch, 6m), null, CancellationToken.None);
        var second = await _stock.Poster.PostAsync(new StockMovement(TransactionType.Sale, _store, _product, _batch, 6m), null, CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeFalse("only 4 are left once the first line has taken 6");
        _stock.OnHand(_store, _product, _batch).Should().Be(4m);
    }

    [Fact]
    public async Task EveryMovement_CarriesItsDocumentAndPrices()
    {
        var documentId = Guid.NewGuid();

        var result = await _stock.Poster.PostAsync(
            new StockMovement(TransactionType.Receipt, _store, _product, _batch, 100m, UnitCost: 1.45m,
                SourceType: StockSourceType.GoodsReceipt, SourceId: documentId, SourceNo: "GRN-2026-000001"),
            null, CancellationToken.None);

        var row = result.Value!;
        row.SourceType.Should().Be(StockSourceType.GoodsReceipt);
        row.SourceId.Should().Be(documentId);
        row.SourceNo.Should().Be("GRN-2026-000001");
        row.UnitCost.Should().Be(1.45m);
    }

    [Theory]
    [InlineData(TransactionType.Receipt, true)]
    [InlineData(TransactionType.OpeningStock, true)]
    [InlineData(TransactionType.SaleReturn, true)]
    [InlineData(TransactionType.TransferIn, true)]
    [InlineData(TransactionType.AdjustmentIn, true)]
    [InlineData(TransactionType.Dispense, false)]
    [InlineData(TransactionType.Sale, false)]
    [InlineData(TransactionType.PurchaseReturn, false)]
    [InlineData(TransactionType.TransferOut, false)]
    [InlineData(TransactionType.AdjustmentOut, false)]
    public void EveryMovementType_HasADirection(TransactionType type, bool inward)
    {
        HMS.Modules.Pharmacy.Domain.PharmacyStockTransaction.IsInward(type).Should().Be(inward);
    }
}
