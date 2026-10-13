using FluentAssertions;
using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Products.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.Pharmacy.Application;

public class PharmacyPricingTests
{
    private static ProductResponse Product(decimal unitsPerPack = 1m, decimal sellingPrice = 0m, decimal costPrice = 0m) =>
        new() { UnitsPerPack = unitsPerPack, SellingPrice = sellingPrice, CostPrice = costPrice };

    private static ProductBatchResponse Batch(decimal? mrp = null, decimal? purchaseRate = null, decimal? sellingRate = null) =>
        new() { Mrp = mrp, PurchaseRate = purchaseRate, SellingRate = sellingRate };

    [Fact]
    public void UnitPrice_WithOneUnitPerPack_IsThePackPrice()
    {
        // How every product behaved before packs existed: the price is the unit price.
        PharmacyPricing.UnitPrice(Product(sellingPrice: 20m), Batch()).Should().Be(20m);
    }

    [Fact]
    public void UnitPrice_DividesThePackPriceByUnitsPerPack()
    {
        PharmacyPricing.UnitPrice(Product(unitsPerPack: 10m), Batch(mrp: 21m)).Should().Be(2.1m);
    }

    [Fact]
    public void UnitPrice_PrefersTheBatchSellingRate_ThenItsMrp_ThenTheProductPrice()
    {
        var product = Product(sellingPrice: 30m);

        PharmacyPricing.UnitPrice(product, Batch(mrp: 24m, sellingRate: 22m)).Should().Be(22m);
        PharmacyPricing.UnitPrice(product, Batch(mrp: 24m)).Should().Be(24m);
        PharmacyPricing.UnitPrice(product, Batch()).Should().Be(30m);
    }

    [Fact]
    public void UnitCost_PrefersTheBatchPurchaseRate_ThenTheProductCost()
    {
        var product = Product(unitsPerPack: 10m, costPrice: 14m);

        PharmacyPricing.UnitCost(product, Batch(purchaseRate: 15m)).Should().Be(1.5m);
        PharmacyPricing.UnitCost(product, Batch()).Should().Be(1.4m);
    }

    [Fact]
    public void UnitCost_IsNullNotZero_WhenNoCostIsRecordedAnywhere()
    {
        PharmacyPricing.UnitCost(Product(), Batch()).Should().BeNull();
    }

    [Fact]
    public void LineTotal_RoundsToPaise_AndAWholePackComesBackToItsPackPrice()
    {
        // A strip of 15 at ₹20: ₹1.3333 a tablet, and 15 tablets must still bill ₹20.00.
        var unitPrice = PharmacyPricing.UnitPrice(Product(unitsPerPack: 15m), Batch(mrp: 20m));

        unitPrice.Should().Be(1.3333m);
        PharmacyPricing.LineTotal(15m, unitPrice).Should().Be(20.00m);
        PharmacyPricing.LineTotal(4m, unitPrice).Should().Be(5.33m);
    }
}
