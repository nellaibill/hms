using FluentAssertions;
using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Products.Domain;

public class ProductDrugProfileTests
{
    private static Product NewProduct() => Product.Create(
        "PARA-500", "P0001", "Paracetamol 500mg", "Paracetamol", null,
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        isBatchTracked: true, isSerialized: false, isActive: true,
        reorderLevel: 0, minStockLevel: 0, maxStockLevel: 0, mrp: 20m, costPrice: 12m, sellingPrice: 20m,
        hsnCode: "3004", weight: null, volume: null, createdBy: null);

    [Fact]
    public void Create_DefaultsToOneUnitPerPack_SoPackAndUnitPricesAreTheSame()
    {
        var product = NewProduct();

        product.UnitsPerPack.Should().Be(1m);
        product.Schedule.Should().Be(DrugSchedule.None);
        product.StorageCondition.Should().Be(StorageCondition.RoomTemperature);
        product.DosageForm.Should().BeNull();
        product.GstRatePercent.Should().BeNull();
    }

    [Fact]
    public void SetDrugProfile_StoresEveryField_TrimmingText()
    {
        var product = NewProduct();

        product.SetDrugProfile(DosageForm.Tablet, " 500 mg ", 10m, DrugSchedule.H1, StorageCondition.CoolPlace, " A-12 ", 5m, updatedBy: null);

        product.DosageForm.Should().Be(DosageForm.Tablet);
        product.Strength.Should().Be("500 mg");
        product.UnitsPerPack.Should().Be(10m);
        product.Schedule.Should().Be(DrugSchedule.H1);
        product.StorageCondition.Should().Be(StorageCondition.CoolPlace);
        product.RackLocation.Should().Be("A-12");
        product.GstRatePercent.Should().Be(5m);
    }

    [Fact]
    public void SetDrugProfile_TreatsBlankTextAsNotSet()
    {
        var product = NewProduct();

        product.SetDrugProfile(null, "  ", 1m, DrugSchedule.None, StorageCondition.RoomTemperature, "", null, updatedBy: null);

        product.Strength.Should().BeNull();
        product.RackLocation.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void SetDrugProfile_RejectsAPackOfNoUnits(decimal unitsPerPack)
    {
        var product = NewProduct();

        var act = () => product.SetDrugProfile(null, null, unitsPerPack, DrugSchedule.None, StorageCondition.RoomTemperature, null, null, updatedBy: null);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100.01)]
    public void SetDrugProfile_RejectsAGstRateOutsideZeroToHundred(decimal gstRatePercent)
    {
        var product = NewProduct();

        var act = () => product.SetDrugProfile(null, null, 1m, DrugSchedule.None, StorageCondition.RoomTemperature, null, gstRatePercent, updatedBy: null);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

public class ProductBatchPriceTests
{
    private static ProductBatch NewBatch() =>
        ProductBatch.Create(Guid.NewGuid(), "B-2026-001", new DateOnly(2026, 1, 1), new DateOnly(2028, 1, 1), isActive: true, createdBy: null);

    [Fact]
    public void Create_HasNoPrices_SoPricingFallsBackToTheProduct()
    {
        var batch = NewBatch();

        batch.Mrp.Should().BeNull();
        batch.PurchaseRate.Should().BeNull();
        batch.SellingRate.Should().BeNull();
    }

    [Fact]
    public void SetPrices_StoresMrpAndRates()
    {
        var batch = NewBatch();

        batch.SetPrices(mrp: 24.50m, purchaseRate: 17.20m, sellingRate: 23m, updatedBy: null);

        batch.Mrp.Should().Be(24.50m);
        batch.PurchaseRate.Should().Be(17.20m);
        batch.SellingRate.Should().Be(23m);
    }

    [Fact]
    public void SetPrices_AllowsSellingAtMrp()
    {
        var batch = NewBatch();

        batch.SetPrices(mrp: 24.50m, purchaseRate: null, sellingRate: 24.50m, updatedBy: null);

        batch.SellingRate.Should().Be(24.50m);
    }

    [Fact]
    public void SetPrices_RefusesASellingRateAboveMrp()
    {
        var batch = NewBatch();

        var act = () => batch.SetPrices(mrp: 24.50m, purchaseRate: null, sellingRate: 24.51m, updatedBy: null);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetPrices_AllowsASellingRateWithNoMrpToCompareAgainst()
    {
        var batch = NewBatch();

        batch.SetPrices(mrp: null, purchaseRate: null, sellingRate: 30m, updatedBy: null);

        batch.SellingRate.Should().Be(30m);
    }

    [Theory]
    [InlineData(-1, null, null)]
    [InlineData(null, -1, null)]
    [InlineData(null, null, -1)]
    public void SetPrices_RefusesNegativeValues(int? mrp, int? purchaseRate, int? sellingRate)
    {
        var batch = NewBatch();

        var act = () => batch.SetPrices(mrp, purchaseRate, sellingRate, updatedBy: null);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
