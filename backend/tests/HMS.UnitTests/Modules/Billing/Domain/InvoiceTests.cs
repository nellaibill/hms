using FluentAssertions;
using HMS.Modules.Billing.Contracts;
using HMS.Modules.Billing.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Billing.Domain;

public class InvoiceTests
{
    private static InvoiceLineItemSpec Spec(
        BillingType billingType = BillingType.Consultation,
        int quantity = 1,
        decimal unitPrice = 500m,
        decimal discount = 0m) =>
        new(billingType, "cardiology", "dr-revathi", "svc", null, quantity, unitPrice, discount, discount > 0, discount > 0 ? "Supervisor" : null);

    [Fact]
    public void Create_ComputesGrossDiscountAndNetFromItems()
    {
        var items = new[]
        {
            Spec(BillingType.Consultation, quantity: 1, unitPrice: 720m),
            Spec(BillingType.Laboratory, quantity: 1, unitPrice: 700m, discount: 100m),
        };

        var invoice = Invoice.Create("INV-2026-000001", Guid.NewGuid(), Guid.NewGuid(), "Aravind Nadar", "NH20260001", items, createdBy: null);

        invoice.GrossAmount.Should().Be(1420m);
        invoice.TotalDiscount.Should().Be(100m);
        invoice.NetAmount.Should().Be(1320m);
        invoice.Items.Should().HaveCount(2);
        invoice.PaymentStatus.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public void Create_EachLineItem_ClampsTotalAtZeroWhenDiscountExceedsCharge()
    {
        // The validator (CreateInvoiceRequestValidator) is what actually rejects this at the
        // API boundary — this test just confirms the domain itself never produces a negative
        // total even if something upstream let an over-large discount through.
        var items = new[] { Spec(unitPrice: 100m, discount: 150m) };

        var invoice = Invoice.Create("INV-2026-000002", Guid.NewGuid(), Guid.NewGuid(), "Test Patient", "NH1", items, createdBy: null);

        invoice.Items.Single().Total.Should().Be(0m);
    }

    [Fact]
    public void PaymentStatus_IsPaid_OnlyWhenEveryLineItemIsPaid()
    {
        var invoice = Invoice.Create(
            "INV-2026-000003",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Patient",
            "NH1",
            [Spec(), Spec(BillingType.Radiology)],
            createdBy: null);

        var firstItemId = invoice.Items.First().Id;
        invoice.MarkItemPaid(firstItemId, updatedBy: null);

        invoice.PaymentStatus.Should().Be(PaymentStatus.Pending, "one line item is still unpaid");

        var secondItemId = invoice.Items.Last().Id;
        invoice.MarkItemPaid(secondItemId, updatedBy: null);

        invoice.PaymentStatus.Should().Be(PaymentStatus.Paid);
    }

    [Fact]
    public void MarkItemPaid_FlipsOnlyTheTargetedLineItem()
    {
        var invoice = Invoice.Create(
            "INV-2026-000004",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Patient",
            "NH1",
            [Spec(), Spec(BillingType.Procedure)],
            createdBy: null);

        var targetId = invoice.Items.First().Id;
        var paidItem = invoice.MarkItemPaid(targetId, updatedBy: null);

        paidItem.Id.Should().Be(targetId);
        paidItem.PaymentStatus.Should().Be(PaymentStatus.Paid);
        invoice.Items.Last().PaymentStatus.Should().Be(PaymentStatus.Pending);
    }
}
