using FluentAssertions;
using HMS.Modules.Pharmacy.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Pharmacy.Domain;

public class PharmacyStockBalanceTests
{
    [Fact]
    public void Create_StartsAtZeroQuantityOnHand()
    {
        var productId = Guid.NewGuid();
        var productBatchId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var balance = PharmacyStockBalance.Create(productId, productBatchId, actorId);

        balance.ProductId.Should().Be(productId);
        balance.ProductBatchId.Should().Be(productBatchId);
        balance.QuantityOnHand.Should().Be(0m);
        balance.CreatedBy.Should().Be(actorId);
        balance.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Receive_IncreasesQuantityOnHandAndAudit()
    {
        var balance = PharmacyStockBalance.Create(Guid.NewGuid(), Guid.NewGuid(), null);
        var updatedBy = Guid.NewGuid();

        balance.Receive(10m, updatedBy);

        balance.QuantityOnHand.Should().Be(10m);
        balance.UpdatedBy.Should().Be(updatedBy);
        balance.UpdatedAt.Should().NotBeNull();

        balance.Receive(5m, updatedBy);

        balance.QuantityOnHand.Should().Be(15m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Receive_WithNonPositiveQuantity_ThrowsArgumentOutOfRangeException(decimal quantity)
    {
        var balance = PharmacyStockBalance.Create(Guid.NewGuid(), Guid.NewGuid(), null);

        var act = () => balance.Receive(quantity, null);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Dispense_DecreasesQuantityOnHandAndAudit()
    {
        var balance = PharmacyStockBalance.Create(Guid.NewGuid(), Guid.NewGuid(), null);
        balance.Receive(20m, null);
        var updatedBy = Guid.NewGuid();

        balance.Dispense(8m, updatedBy);

        balance.QuantityOnHand.Should().Be(12m);
        balance.UpdatedBy.Should().Be(updatedBy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Dispense_WithNonPositiveQuantity_ThrowsArgumentOutOfRangeException(decimal quantity)
    {
        var balance = PharmacyStockBalance.Create(Guid.NewGuid(), Guid.NewGuid(), null);
        balance.Receive(10m, null);

        var act = () => balance.Dispense(quantity, null);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Dispense_WhenQuantityExceedsQuantityOnHand_ThrowsInvalidOperationException()
    {
        var balance = PharmacyStockBalance.Create(Guid.NewGuid(), Guid.NewGuid(), null);
        balance.Receive(5m, null);

        var act = () => balance.Dispense(6m, null);

        act.Should().Throw<InvalidOperationException>();
        balance.QuantityOnHand.Should().Be(5m);
    }

    [Fact]
    public void Dispense_WhenQuantityEqualsQuantityOnHand_ReducesToZero()
    {
        var balance = PharmacyStockBalance.Create(Guid.NewGuid(), Guid.NewGuid(), null);
        balance.Receive(5m, null);

        balance.Dispense(5m, null);

        balance.QuantityOnHand.Should().Be(0m);
    }
}
