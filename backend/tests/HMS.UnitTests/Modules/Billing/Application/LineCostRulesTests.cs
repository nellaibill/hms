using FluentAssertions;
using HMS.Modules.Billing.Application;
using HMS.Modules.Billing.Contracts;
using HMS.Modules.Billing.Domain;
using HMS.Modules.Masters.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.Billing.Application;

/// <summary>ADR-153: the cost a bill line keeps from billing time. These must match
/// billingCalculations.ts's resolveItemCostPrice / resolveItemCostSplit, which the reports use for
/// a line without a stored cost.</summary>
public class LineCostRulesTests
{
    private static readonly Guid Regular = Guid.NewGuid();
    private static readonly Guid Emergency = Guid.NewGuid();

    private static ConsultantResponse Meena() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Dr. S. Meena",
        ConsultationTypeCharges = [new ConsultationTypeChargeDto(Regular, 250m), new ConsultationTypeChargeDto(Emergency, 0m)],
    };

    [Fact]
    public void Consultation_IsTheConsultantsShareForThatType()
        => LineCostRules.Consultation(Meena(), Regular).Should().Be(new LineCost(250m, null));

    [Fact]
    public void Consultation_ShareOfZero_IsARealShare()
        => LineCostRules.Consultation(Meena(), Emergency).Should().Be(new LineCost(0m, null));

    [Fact]
    public void Consultation_TypeNotOfferedOrShareBlankOrNoConsultant_IsUnknown()
    {
        LineCostRules.Consultation(Meena(), Guid.NewGuid()).Should().BeNull();
        LineCostRules.Consultation(Meena() with { ConsultationTypeCharges = [new ConsultationTypeChargeDto(Regular, null)] }, Regular).Should().BeNull();
        LineCostRules.Consultation(null, Regular).Should().BeNull();
    }

    private static readonly Guid Lh = Guid.NewGuid();

    private static DiagnosticServiceResponse GrowthScan() => new()
    {
        Id = Guid.NewGuid(),
        Name = "ANC - Growth Scan",
        DoctorCharge = 300m,
        CostPrice = 150m,
        BedsidePrice = 500m,
        BedsideDoctorCharge = 200m,
        RateOptions = [new DiagnosticServiceRateOptionDto { Id = Lh, Name = "LH", Price = 800m, DoctorCharge = 250m, CostPrice = 100m }],
    };

    [Fact]
    public void Radiology_StandardRate_IsDoctorsSharePlusRunningCost()
        => LineCostRules.Radiology(GrowthScan(), null, isBedside: false, null).Should().Be(new LineCost(450m, 300m));

    [Fact]
    public void Radiology_RateOption_UsesTheOptionsShareAndRunningCost()
        => LineCostRules.Radiology(GrowthScan(), Lh, isBedside: false, null).Should().Be(new LineCost(350m, 250m));

    [Fact]
    public void Radiology_RemovedRateOption_FallsBackToTheStandardRate()
        => LineCostRules.Radiology(GrowthScan(), Guid.NewGuid(), isBedside: false, null).Should().Be(new LineCost(450m, 300m));

    [Fact]
    public void Radiology_Bedside_AddsTheLinesOwnBedsideShare_ElseTheServices()
    {
        LineCostRules.Radiology(GrowthScan(), null, isBedside: true, billedBedsideDoctorCharge: 350m).Should().Be(new LineCost(800m, 650m));
        LineCostRules.Radiology(GrowthScan(), null, isBedside: true, billedBedsideDoctorCharge: null).Should().Be(new LineCost(650m, 500m));
    }

    [Fact]
    public void Radiology_NotCostedOrNotFound_IsUnknown()
    {
        LineCostRules.Radiology(GrowthScan() with { DoctorCharge = 0m, CostPrice = 0m, RateOptions = [] }, null, isBedside: true, 300m).Should().BeNull();
        LineCostRules.Radiology(null, null, isBedside: false, null).Should().BeNull();
    }

    [Fact]
    public void LabTest_IsItsRunningCost_ZeroMeaningNotYetCosted()
    {
        LineCostRules.LabTest(new DiagnosticServiceResponse { CostPrice = 90m }).Should().Be(new LineCost(90m, null));
        LineCostRules.LabTest(new DiagnosticServiceResponse { CostPrice = 0m }).Should().BeNull();
        LineCostRules.LabTest(null).Should().BeNull();
    }

    private static readonly Guid Cbc = Guid.NewGuid();
    private static readonly Guid Esr = Guid.NewGuid();

    private static DiagnosticPackageResponse FeverProfile(decimal costPrice) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Fever Profile",
        CostPrice = costPrice,
        Items = [new DiagnosticPackageItemResponse { ServiceId = Cbc }, new DiagnosticPackageItemResponse { ServiceId = Esr }],
    };

    private static Dictionary<Guid, DiagnosticServiceResponse> Tests(decimal cbc, decimal esr) => new()
    {
        [Cbc] = new DiagnosticServiceResponse { Id = Cbc, CostPrice = cbc },
        [Esr] = new DiagnosticServiceResponse { Id = Esr, CostPrice = esr },
    };

    [Fact]
    public void LabPackage_OwnRunningCostWins()
        => LineCostRules.LabPackage(FeverProfile(200m), Tests(90m, 30m)).Should().Be(new LineCost(200m, null));

    [Fact]
    public void LabPackage_WithoutOwnCost_IsItsTestsTotal_OnlyWhenEveryTestIsCosted()
    {
        LineCostRules.LabPackage(FeverProfile(0m), Tests(90m, 30m)).Should().Be(new LineCost(120m, null));
        LineCostRules.LabPackage(FeverProfile(0m), Tests(90m, 0m)).Should().BeNull();
        LineCostRules.LabPackage(FeverProfile(0m), new Dictionary<Guid, DiagnosticServiceResponse> { [Cbc] = new() { Id = Cbc, CostPrice = 90m } }).Should().BeNull();
        LineCostRules.LabPackage(FeverProfile(0m) with { Items = [] }, Tests(90m, 30m)).Should().BeNull();
        LineCostRules.LabPackage(null, Tests(90m, 30m)).Should().BeNull();
    }

    [Fact]
    public void CostPrice_ZeroOrMissing_IsUnknown()
    {
        LineCostRules.CostPrice(350m).Should().Be(new LineCost(350m, null));
        LineCostRules.CostPrice(0m).Should().BeNull();
        LineCostRules.CostPrice(null).Should().BeNull();
    }

    [Fact]
    public void InvoiceLine_KeepsTheDoctorsShareOnlyOnAScan()
    {
        var invoice = Invoice.Create(
            "INV-2026-000001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Sankaralingam Perumal",
            "P-2026-040009",
            [
                new InvoiceLineItemSpec(BillingType.Consultation, null, null, null, null, 1, 500m, 0m, false, null, UnitCost: 400m, UnitDoctorShare: 400m),
                new InvoiceLineItemSpec(BillingType.Radiology, null, null, null, null, 1, 1000m, 0m, false, null, UnitCost: 450m, UnitDoctorShare: 300m),
                new InvoiceLineItemSpec(BillingType.Radiology, null, null, null, null, 1, 1000m, 0m, false, null, UnitCost: null, UnitDoctorShare: 300m),
            ],
            createdBy: null);

        invoice.Items.Select(i => (i.UnitCost, i.UnitDoctorShare)).Should().Equal((400m, null), (450m, 300m), (null, null));
    }
}
