using FluentAssertions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.IPD.Domain;

public class MedicationOrderTests
{
    private readonly Guid _admissionId = Guid.NewGuid();

    [Fact]
    public void Create_StartsInActiveStatus()
    {
        var order = MedicationOrder.Create(_admissionId, "Ceftriaxone", "1g", "IV", "BD", DateTime.UtcNow, null, null, DateTime.UtcNow, null, null);

        order.Status.Should().Be(MedicationOrderStatus.Active);
        order.DrugName.Should().Be("Ceftriaxone");
    }

    [Theory]
    [InlineData("", "1g", "IV", "BD")]
    [InlineData("Ceftriaxone", "", "IV", "BD")]
    [InlineData("Ceftriaxone", "1g", "", "BD")]
    [InlineData("Ceftriaxone", "1g", "IV", "")]
    public void Create_WithBlankRequiredField_Throws(string drugName, string dose, string route, string frequency)
    {
        var act = () => MedicationOrder.Create(_admissionId, drugName, dose, route, frequency, DateTime.UtcNow, null, null, DateTime.UtcNow, null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Discontinue_FromActive_Succeeds()
    {
        var order = MedicationOrder.Create(_admissionId, "Paracetamol", "500mg", "PO", "TDS", DateTime.UtcNow, null, null, DateTime.UtcNow, null, null);

        order.Discontinue("Fever resolved", null);

        order.Status.Should().Be(MedicationOrderStatus.Discontinued);
        order.DiscontinuedReason.Should().Be("Fever resolved");
        order.DiscontinuedAt.Should().NotBeNull();
    }

    [Fact]
    public void Discontinue_WhenAlreadyDiscontinued_Throws()
    {
        var order = MedicationOrder.Create(_admissionId, "Paracetamol", "500mg", "PO", "TDS", DateTime.UtcNow, null, null, DateTime.UtcNow, null, null);
        order.Discontinue(null, null);

        var act = () => order.Discontinue(null, null);

        act.Should().Throw<InvalidOperationException>();
    }
}
