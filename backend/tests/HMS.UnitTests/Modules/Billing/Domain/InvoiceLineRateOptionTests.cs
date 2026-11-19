using FluentAssertions;
using FluentValidation.TestHelper;
using HMS.Modules.Billing.Application.Validators;
using HMS.Modules.Billing.Contracts;
using HMS.Modules.Billing.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Billing.Domain;

/// <summary>ADR-140: a radiology bill line remembers the rate option billed and whether the scan
/// was done at the bedside.</summary>
public class InvoiceLineRateOptionTests
{
    [Fact]
    public void Create_KeepsTheRateOptionAndBedsideOnTheLine()
    {
        var optionId = Guid.NewGuid();
        var spec = new InvoiceLineItemSpec(
            BillingType.Radiology, null, "dr-x", "svc", null, 1, 1800m, 0m, false, null,
            RateOptionId: optionId, RateOptionName: " LH ", IsBedside: true);

        var invoice = Invoice.Create("INV-2026-000010", Guid.NewGuid(), Guid.NewGuid(), "Test Patient", "NH1", [spec], createdBy: null);

        var line = invoice.Items.Single();
        line.RateOptionId.Should().Be(optionId);
        line.RateOptionName.Should().Be("LH");
        line.IsBedside.Should().BeTrue();
        line.Total.Should().Be(1800m);
    }

    [Fact]
    public void Create_WithoutRateOption_LeavesTheStandardRate()
    {
        var spec = new InvoiceLineItemSpec(BillingType.Radiology, null, "dr-x", "svc", null, 1, 2200m, 0m, false, null);

        var invoice = Invoice.Create("INV-2026-000011", Guid.NewGuid(), Guid.NewGuid(), "Test Patient", "NH1", [spec], createdBy: null);

        var line = invoice.Items.Single();
        line.RateOptionId.Should().BeNull();
        line.RateOptionName.Should().BeNull();
        line.IsBedside.Should().BeFalse();
    }

    [Theory]
    [InlineData(BillingType.Laboratory)]
    [InlineData(BillingType.Consultation)]
    public void Validator_RejectsBedsideOrRateOptionOffRadiology(BillingType billingType)
    {
        var validator = new CreateInvoiceLineItemRequestValidator();
        var line = new CreateInvoiceLineItemRequest { BillingType = billingType, ServiceId = "svc", Quantity = 1, UnitPrice = 100m, IsBedside = true };

        validator.TestValidate(line).ShouldHaveValidationErrorFor(x => x.BillingType);
    }

    [Fact]
    public void Validator_AcceptsARadiologyLineWithRateOptionAndBedside()
    {
        var validator = new CreateInvoiceLineItemRequestValidator();
        var line = new CreateInvoiceLineItemRequest
        {
            BillingType = BillingType.Radiology,
            ServiceId = "svc",
            RateOptionId = Guid.NewGuid(),
            RateOptionName = "MR",
            IsBedside = true,
            Quantity = 1,
            UnitPrice = 1800m,
        };

        validator.TestValidate(line).IsValid.Should().BeTrue();
    }
}
