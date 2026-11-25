using FluentAssertions;
using HMS.Modules.Billing.Application.Mapping;
using HMS.Modules.Billing.Contracts;
using HMS.Modules.Billing.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Billing.Application;

// ADR-148: a consultant's own login sees only the bill lines billed to them, with the bill's
// totals worked out from those lines — never another doctor's charges or the bill's full total.
public class InvoiceConsultantScopeTests
{
    private static InvoiceResponse BillForTwoDoctors() => Invoice.Create(
        "INV-2026-000003",
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Jeyarani Thangaraj",
        "P-2026-040003",
        [
            new InvoiceLineItemSpec(BillingType.Consultation, "obgyn", "dr-meena", "emergency", null, 1, 700m, 0m, false, null),
            new InvoiceLineItemSpec(BillingType.Radiology, null, "dr-meena", "anc-scan", null, 1, 2000m, 0m, false, null),
            new InvoiceLineItemSpec(BillingType.Consultation, "obgyn", "dr-madhavi", "priority", null, 1, 400m, 40m, true, "superadmin"),
        ],
        createdBy: null).ToResponse();

    [Fact]
    public void ForConsultant_KeepsOnlyThatConsultantsLines_AndTotalsThemAlone()
    {
        var own = BillForTwoDoctors().ForConsultant("dr-meena");

        own.Items.Should().HaveCount(2).And.OnlyContain(i => i.BilledConsultantId == "dr-meena");
        own.GrossAmount.Should().Be(2700m);
        own.TotalDiscount.Should().Be(0m);
        own.NetAmount.Should().Be(2700m);
    }

    [Fact]
    public void ForConsultant_CarriesTheLinesOwnDiscountIntoTheTotals()
    {
        var own = BillForTwoDoctors().ForConsultant("dr-madhavi");

        own.Items.Should().ContainSingle();
        own.GrossAmount.Should().Be(400m);
        own.NetAmount.Should().Be(own.Items.Single().Total);
        own.TotalDiscount.Should().Be(own.GrossAmount - own.NetAmount);
    }

    [Fact]
    public void ForConsultant_WithNoLineOfTheirs_LeavesNothing()
    {
        var own = BillForTwoDoctors().ForConsultant("dr-lakshmanan");

        own.Items.Should().BeEmpty();
        own.NetAmount.Should().Be(0m);
    }
}
