using FluentAssertions;
using HMS.Modules.ActivityLog.Application;
using HMS.Modules.ActivityLog.Contracts;
using HMS.Modules.Billing.Application;
using HMS.Modules.Billing.Contracts;
using HMS.Modules.Billing.Domain;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Masters.Application;

/// <summary>ADR-143: price and charge changes in Masters are logged with a readable reference and
/// a description of what changed; bills read by invoice number and patient.</summary>
public class MastersActivityLogTests
{
    private static readonly Guid CategoryId = Guid.NewGuid();

    [Fact]
    public async Task UpdatingARadiologyServicesPrice_LogsTheNameAndTheChange()
    {
        var repository = Substitute.For<IDiagnosticServiceRepository>();
        var categories = Substitute.For<IDiagnosticCategoryService>();
        categories.GetByIdAsync(CategoryId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticCategoryResponse>.Success(new DiagnosticCategoryResponse { Id = CategoryId }));
        var activityLog = Substitute.For<IActivityLogService>();
        var sut = new DiagnosticServiceService(repository, categories, Substitute.For<IDiagnosticProviderService>(), activityLog);

        var scan = DiagnosticService.Create("ANCGROWTH", "ANC - Growth Scan", CategoryId, DiagnosticTestServiceType.Radiology, false, null, 2200m, 200m, true, null);
        scan.SetTariff(800m, null, null, []);
        repository.GetByIdAsync(scan.Id, Arg.Any<CancellationToken>()).Returns(scan);

        var result = await sut.UpdateAsync(scan.Id, new UpdateDiagnosticServiceRequest
        {
            Code = "ANCGROWTH",
            Name = "ANC - Growth Scan",
            CategoryId = CategoryId,
            ServiceType = DiagnosticTestServiceType.Radiology,
            Price = 2400m,
            DoctorCharge = 800m,
            CostPrice = 200m,
            IsActive = true,
        }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await activityLog.Received(1).LogAsync(
            Arg.Is<ActivityLogRequest>(r =>
                r.Module == "Masters"
                && r.Action == ActivityLogActions.Update
                && r.EntityReference == "ANC - Growth Scan"
                && r.Description == "Updated radiology service 'ANC - Growth Scan' — price ₹2,200 → ₹2,400"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangingAConsultantsCharge_LogsTheConsultationTypeByName()
    {
        var repository = Substitute.For<IConsultantRepository>();
        var types = Substitute.For<IConsultationTypeRepository>();
        var activityLog = Substitute.For<IActivityLogService>();
        var sut = new ConsultantService(repository, Substitute.For<IDepartmentRepository>(), types, Substitute.For<IConsultantFileStorage>(), activityLog);

        var regular = ConsultationType.Create("Doctor's Consultation (In-house) - Regular", "Regular", 250m, true, null);
        types.ExistsAsync(regular.Id, Arg.Any<CancellationToken>()).Returns(true);
        types.GetByIdAsync(regular.Id, Arg.Any<CancellationToken>()).Returns(regular);
        var days = new[] { "Monday" };
        var consultant = Consultant.Create("Dr. L. Madhavi", null, null, true, null, days, new TimeOnly(10, 0), new TimeOnly(16, 0),
            [new ConsultationTypeSelection(regular.Id, 200m)], null);
        repository.GetByIdAsync(consultant.Id, Arg.Any<CancellationToken>()).Returns(consultant);

        var result = await sut.UpdateAsync(consultant.Id, new UpdateConsultantRequest
        {
            Name = "Dr. L. Madhavi",
            IsActive = true,
            AvailableDays = days,
            VisitStartTime = new TimeOnly(10, 0),
            VisitEndTime = new TimeOnly(16, 0),
            ConsultationTypeCharges = [new ConsultationTypeChargeDto(regular.Id, 250m)],
        }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await activityLog.Received(1).LogAsync(
            Arg.Is<ActivityLogRequest>(r =>
                r.EntityType == "Consultant"
                && r.EntityReference == "Dr. L. Madhavi"
                && r.Description == "Updated consultant 'Dr. L. Madhavi' — Doctor's Consultation (In-house) - Regular ₹200 → ₹250"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void MoneyAndChanges_ReadAsRupees()
    {
        MastersActivityLog.Money(1300m).Should().Be("₹1,300");
        MastersActivityLog.Money(12.5m).Should().Be("₹12.5");
        MastersActivityLog.Money(null).Should().Be("not set");
        MastersActivityLog.Changes(("price", 50m, 60m), ("running cost", 35m, 35m)).Should().Be(" — price ₹50 → ₹60");
        MastersActivityLog.Changes(("price", 50m, 50m)).Should().BeEmpty();
    }

    [Fact]
    public void CreatedInvoice_NamesThePatientAndTheAmount()
    {
        var spec = new InvoiceLineItemSpec(BillingType.Laboratory, null, "dr-x", "svc", null, 1, 1300m, 0m, false, null);
        var invoice = Invoice.Create("INV-2026-000288", Guid.NewGuid(), Guid.NewGuid(), "Ramesh Anandh", "P-2026-040148", [spec], createdBy: null);

        InvoiceService.DescribeCreated(invoice).Should().Be("Created invoice INV-2026-000288 for Ramesh Anandh (P-2026-040148) — ₹1,300.00");
    }
}
