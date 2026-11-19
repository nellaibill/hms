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
    public void Create_KeepsTheBedsideAmountAndDoctorChargeOnABedsideLine()
    {
        var spec = new InvoiceLineItemSpec(
            BillingType.Radiology, null, "dr-x", "svc", null, 1, 1600m, 0m, false, null,
            IsBedside: true, BedsideAmount: 600m, BedsideDoctorCharge: 300m);

        var line = Invoice.Create("INV-2026-000012", Guid.NewGuid(), Guid.NewGuid(), "Test Patient", "NH1", [spec], createdBy: null).Items.Single();

        line.BedsideAmount.Should().Be(600m);
        line.BedsideDoctorCharge.Should().Be(300m);
    }

    [Fact]
    public void Create_DropsABedsideAmountOffALineThatIsNotBedside()
    {
        var spec = new InvoiceLineItemSpec(
            BillingType.Radiology, null, "dr-x", "svc", null, 1, 1000m, 0m, false, null,
            IsBedside: false, BedsideAmount: 600m, BedsideDoctorCharge: 300m);

        var line = Invoice.Create("INV-2026-000013", Guid.NewGuid(), Guid.NewGuid(), "Test Patient", "NH1", [spec], createdBy: null).Items.Single();

        line.BedsideAmount.Should().BeNull();
        line.BedsideDoctorCharge.Should().BeNull();
    }

    [Theory]
    [InlineData(500, 200, 300)]
    [InlineData(500, 0, 500)]
    [InlineData(200, 500, 0)]
    public void BedsideShare_HospitalShareIsThePriceLessTheDoctorsPart(decimal bedsidePrice, decimal doctorCharge, decimal expected)
        => BedsideShare.HospitalShare(bedsidePrice, doctorCharge).Should().Be(expected);

    [Theory]
    [InlineData(500, 300, 200)]
    [InlineData(600, 300, 300)]
    [InlineData(300, 300, 0)]
    public void BedsideShare_ConsultantGetsWhatIsAboveTheHospitalsShare(decimal amount, decimal hospitalShare, decimal expected)
        => BedsideShare.DoctorCharge(amount, hospitalShare).Should().Be(expected);

    [Fact]
    public void BedsideShare_AnAmountBelowTheHospitalsShareHasNoSplit()
        => BedsideShare.DoctorCharge(299m, 300m).Should().BeNull();

    [Fact]
    public void Validator_RejectsABedsideAmountOnALineThatIsNotBedside()
    {
        var validator = new CreateInvoiceLineItemRequestValidator();
        var line = new CreateInvoiceLineItemRequest { BillingType = BillingType.Radiology, ServiceId = "svc", Quantity = 1, UnitPrice = 1600m, BedsideAmount = 600m };

        validator.TestValidate(line).ShouldHaveValidationErrorFor(x => x.BedsideAmount);
    }

    [Fact]
    public void Validator_RejectsACharge_ThatDoesNotIncludeTheBedsideAmount()
    {
        var validator = new CreateInvoiceLineItemRequestValidator();
        var line = new CreateInvoiceLineItemRequest { BillingType = BillingType.Radiology, ServiceId = "svc", Quantity = 1, UnitPrice = 500m, IsBedside = true, BedsideAmount = 600m };

        validator.TestValidate(line).ShouldHaveValidationErrorFor(x => x.UnitPrice);
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
