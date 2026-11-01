using FluentAssertions;
using HMS.Modules.ActivityLog.Application;
using HMS.Modules.Billing.Application;
using HMS.Modules.Billing.Application.Abstractions;
using HMS.Modules.Billing.Contracts;
using HMS.Modules.Billing.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Billing.Application;

public class InvoiceDiscountServiceTests
{
    private readonly IInvoiceRepository _invoiceRepository = Substitute.For<IInvoiceRepository>();
    private readonly IInvoiceDiscountRequestRepository _repository = Substitute.For<IInvoiceDiscountRequestRepository>();
    private readonly InvoiceDiscountService _sut;

    private static readonly Guid DrKarthikeyan = Guid.NewGuid();
    private static readonly Guid DrParimalam = Guid.NewGuid();
    private static readonly BillingActor Receptionist = new(Guid.NewGuid(), "reception1", IsSuperAdmin: false);
    private static readonly BillingActor SuperAdmin = new(Guid.NewGuid(), "superadmin", IsSuperAdmin: true);
    private static readonly BillingActor Karthikeyan = new(Guid.NewGuid(), "karthikeyan", IsSuperAdmin: false, ConsultantId: DrKarthikeyan);
    private static readonly BillingActor Parimalam = new(Guid.NewGuid(), "parimalam", IsSuperAdmin: false, ConsultantId: DrParimalam);

    public InvoiceDiscountServiceTests()
    {
        _sut = new InvoiceDiscountService(_invoiceRepository, _repository, Substitute.For<IActivityLogService>());
        _repository.GetByInvoiceIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    private static InvoiceLineItemSpec Spec(BillingType billingType = BillingType.Consultation, decimal unitPrice = 500m, decimal discount = 0m, Guid? consultantId = null) =>
        new(billingType, null, consultantId?.ToString(), null, null, 1, unitPrice, discount, false, null);

    /// <summary>An unpaid ₹500 consultation (Dr Karthikeyan) + ₹300 lab bill, unless other lines are given.</summary>
    private Invoice GivenInvoice(params InvoiceLineItemSpec[] items)
    {
        var invoice = Invoice.Create("INV-2026-000200", Guid.NewGuid(), Guid.NewGuid(), "Aravind Nadar", "NH20260001",
            items.Length > 0 ? items : [Spec(consultantId: DrKarthikeyan), Spec(BillingType.Laboratory, 300m)], createdBy: null);
        _invoiceRepository.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        return invoice;
    }

    private static CreateInvoiceDiscountRequest RequestFor(Invoice invoice, params decimal[] discounts) => new()
    {
        DiscountType = InvoiceDiscountType.Amount,
        DiscountValue = discounts.Sum(),
        Reason = "  Staff family  ",
        Lines = invoice.Items.Zip(discounts, (line, discount) => new InvoiceDiscountLineRequest { LineItemId = line.Id, Discount = discount }).Where(l => l.Discount > 0).ToList(),
    };

    /// <summary>A Pending request covering every line of <paramref name="invoice"/>, with the given
    /// discounts in line order.</summary>
    private InvoiceDiscountRequest GivenPendingRequest(Invoice invoice, params decimal[] discounts)
    {
        var lines = invoice.Items.Zip(discounts, (line, discount) => (line, discount)).Where(x => x.discount > 0).ToList();
        var request = InvoiceDiscountRequest.Create(invoice, lines, InvoiceDiscountType.Amount, discounts.Sum(), "Staff family", "reception1", Guid.NewGuid());
        invoice.MarkDiscountPending(null);
        _repository.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        foreach (var approval in request.Approvals)
        {
            _repository.GetByApprovalIdAsync(approval.Id, Arg.Any<CancellationToken>()).Returns(request);
        }

        return request;
    }

    private static InvoiceDiscountApproval ConsultantPart(InvoiceDiscountRequest request, Guid consultantId) =>
        request.Approvals.Single(a => a.ConsultantId == consultantId);

    private static InvoiceDiscountApproval SuperAdminPart(InvoiceDiscountRequest request) =>
        request.Approvals.Single(a => a.ApproverType == InvoiceDiscountApproverType.SuperAdmin);

    [Fact]
    public async Task CreateAsync_FromAReceptionist_RaisesAPendingRequestAndLeavesTheBillAtFullPrice()
    {
        var invoice = GivenInvoice();

        var result = await _sut.CreateAsync(invoice.Id, RequestFor(invoice, 50m, 30m), Receptionist, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(InvoiceDiscountRequestStatus.Pending);
        result.Value.Reason.Should().Be("Staff family");
        result.Value.TotalDiscount.Should().Be(80m);
        result.Value.ApprovedDiscount.Should().Be(0m);
        result.Value.GrossAmount.Should().Be(800m);
        result.Value.RequestedByName.Should().Be("reception1");
        result.Value.Lines.Should().HaveCount(2);
        result.Value.Lines[0].LineAmount.Should().Be(500m);
        invoice.NetAmount.Should().Be(800m);
        invoice.DiscountStatus.Should().Be(InvoiceDiscountRequestStatus.Pending);
        await _repository.Received(1).AddAsync(Arg.Any<InvoiceDiscountRequest>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_SendsAConsultationFeeToItsConsultantAndEveryOtherLineToTheSuperAdmin()
    {
        var invoice = GivenInvoice(
            Spec(consultantId: DrKarthikeyan),
            Spec(BillingType.Radiology, 3000m, consultantId: DrKarthikeyan),
            Spec(BillingType.Laboratory, 200m));

        var result = await _sut.CreateAsync(invoice.Id, RequestFor(invoice, 200m, 1500m, 100m), Receptionist, CancellationToken.None);

        var approvals = result.Value!.Approvals;
        approvals.Should().HaveCount(2);
        approvals[0].ApproverType.Should().Be(InvoiceDiscountApproverType.Consultant);
        approvals[0].ConsultantId.Should().Be(DrKarthikeyan);
        approvals[0].TotalDiscount.Should().Be(200m);
        approvals[0].Status.Should().Be(InvoiceDiscountRequestStatus.Pending);
        // A radiology line naming a doctor is still hospital income — the Super Admin decides it.
        approvals[1].ApproverType.Should().Be(InvoiceDiscountApproverType.SuperAdmin);
        approvals[1].ConsultantId.Should().BeNull();
        approvals[1].TotalDiscount.Should().Be(1600m);
        result.Value.Lines.Single(l => l.BillingType == BillingType.Consultation).ApprovalId.Should().Be(approvals[0].Id);
        result.Value.Lines.Where(l => l.BillingType != BillingType.Consultation).Should().OnlyContain(l => l.ApprovalId == approvals[1].Id);
    }

    [Fact]
    public async Task CreateAsync_GivesEachConsultantTheirOwnPart()
    {
        var invoice = GivenInvoice(Spec(consultantId: DrKarthikeyan), Spec(unitPrice: 400m, consultantId: DrParimalam));

        var result = await _sut.CreateAsync(invoice.Id, RequestFor(invoice, 100m, 50m), Receptionist, CancellationToken.None);

        result.Value!.Approvals.Select(a => a.ConsultantId).Should().Equal(DrKarthikeyan, DrParimalam);
    }

    [Fact]
    public async Task CreateAsync_ForAConsultationWithNoConsultant_SendsItToTheSuperAdmin()
    {
        var invoice = GivenInvoice(Spec());

        var result = await _sut.CreateAsync(invoice.Id, RequestFor(invoice, 100m), Receptionist, CancellationToken.None);

        result.Value!.Approvals.Should().ContainSingle().Which.ApproverType.Should().Be(InvoiceDiscountApproverType.SuperAdmin);
    }

    [Fact]
    public async Task CreateAsync_FromASuperAdmin_AppliesTheDiscountAtOnceAndDecidesTheConsultantsPartOnTheirBehalf()
    {
        var invoice = GivenInvoice();

        var result = await _sut.CreateAsync(invoice.Id, RequestFor(invoice, 50m, 30m), SuperAdmin, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(InvoiceDiscountRequestStatus.Approved);
        result.Value.DecidedByName.Should().Be("superadmin");
        result.Value.ApprovedDiscount.Should().Be(80m);
        result.Value.Approvals.Should().OnlyContain(a => a.Status == InvoiceDiscountRequestStatus.Approved);
        result.Value.Approvals.Single(a => a.ApproverType == InvoiceDiscountApproverType.Consultant).DecidedOnBehalf.Should().BeTrue();
        result.Value.Approvals.Single(a => a.ApproverType == InvoiceDiscountApproverType.SuperAdmin).DecidedOnBehalf.Should().BeFalse();
        invoice.NetAmount.Should().Be(720m);
        invoice.DiscountStatus.Should().Be(InvoiceDiscountRequestStatus.Approved);
        invoice.Items.First().DiscountApprovedBy.Should().Be("superadmin");
    }

    [Fact]
    public async Task CreateAsync_WhileAnotherRequestIsPending_IsAConflict()
    {
        var invoice = GivenInvoice();
        invoice.MarkDiscountPending(null);

        var result = await _sut.CreateAsync(invoice.Id, RequestFor(invoice, 50m), Receptionist, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.DiscountAlreadyPending);
        await _repository.DidNotReceive().AddAsync(Arg.Any<InvoiceDiscountRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_OnAPaidLine_IsNotEligible()
    {
        var invoice = GivenInvoice();
        invoice.MarkItemPaid(invoice.Items.First().Id, null);

        var result = await _sut.CreateAsync(invoice.Id, RequestFor(invoice, 50m), Receptionist, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.DiscountNotEligible);
    }

    [Fact]
    public async Task CreateAsync_OnALineThatAlreadyHasADiscount_IsNotEligible()
    {
        var invoice = GivenInvoice(Spec(discount: 50m), Spec(BillingType.Laboratory, 300m));

        var result = await _sut.CreateAsync(invoice.Id, RequestFor(invoice, 20m), Receptionist, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.DiscountNotEligible);
    }

    [Fact]
    public async Task CreateAsync_OnAPharmacyLine_IsNotEligible()
    {
        var invoice = GivenInvoice(Spec(BillingType.Pharmacy, 400m));

        var result = await _sut.CreateAsync(invoice.Id, RequestFor(invoice, 20m), Receptionist, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.DiscountNotEligible);
    }

    [Fact]
    public async Task CreateAsync_WithADiscountAboveTheLinesCharge_IsNotEligible()
    {
        var invoice = GivenInvoice();

        var result = await _sut.CreateAsync(invoice.Id, RequestFor(invoice, 501m), Receptionist, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.DiscountNotEligible);
    }

    [Fact]
    public async Task CreateAsync_OnAVoidedBill_IsNotEligible()
    {
        var invoice = GivenInvoice();
        invoice.Void("Wrong patient", null);

        var result = await _sut.CreateAsync(invoice.Id, RequestFor(invoice, 50m), Receptionist, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.DiscountNotEligible);
    }

    [Fact]
    public async Task GetForInvoiceAsync_WhileARequestIsPending_SaysANewOneCantBeRaised()
    {
        var invoice = GivenInvoice();
        var pending = GivenPendingRequest(invoice, 100m);
        _repository.GetByInvoiceIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns([pending]);

        var result = await _sut.GetForInvoiceAsync(invoice.Id, CancellationToken.None);

        result.Value!.CanRequest.Should().BeFalse();
        result.Value.IneligibleReason.Should().Contain("already waiting");
        result.Value.Requests.Should().ContainSingle().Which.Id.Should().Be(pending.Id);
    }

    [Fact]
    public async Task ApproveAsync_ByTheConsultant_DiscountsTheirFeeButKeepsTheBillWaitingForTheSuperAdmin()
    {
        var invoice = GivenInvoice();
        var request = GivenPendingRequest(invoice, 100m, 50m);

        var result = await _sut.ApproveAsync(ConsultantPart(request, DrKarthikeyan).Id, new ApproveInvoiceDiscountRequest { Notes = "OK" }, Karthikeyan, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(InvoiceDiscountRequestStatus.Pending);
        result.Value.ApprovedDiscount.Should().Be(100m);
        var part = result.Value.Approvals.Single(a => a.ConsultantId == DrKarthikeyan);
        part.Status.Should().Be(InvoiceDiscountRequestStatus.Approved);
        part.DecidedByName.Should().Be("karthikeyan");
        part.DecisionNotes.Should().Be("OK");
        part.DecidedOnBehalf.Should().BeFalse();
        invoice.Items.First().Total.Should().Be(400m);
        invoice.Items.Last().Total.Should().Be(300m);
        invoice.NetAmount.Should().Be(700m);
        invoice.DiscountStatus.Should().Be(InvoiceDiscountRequestStatus.Pending);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveAsync_OnceEveryPartIsApproved_SettlesTheRequestAndTheBill()
    {
        var invoice = GivenInvoice();
        var request = GivenPendingRequest(invoice, 100m, 50m);
        await _sut.ApproveAsync(ConsultantPart(request, DrKarthikeyan).Id, new ApproveInvoiceDiscountRequest(), Karthikeyan, CancellationToken.None);

        var result = await _sut.ApproveAsync(SuperAdminPart(request).Id, new ApproveInvoiceDiscountRequest(), SuperAdmin, CancellationToken.None);

        result.Value!.Status.Should().Be(InvoiceDiscountRequestStatus.Approved);
        result.Value.DecidedByName.Should().Be("superadmin");
        invoice.NetAmount.Should().Be(650m);
        invoice.DiscountStatus.Should().Be(InvoiceDiscountRequestStatus.Approved);
    }

    [Fact]
    public async Task ApproveAsync_ByTheSuperAdminOnAConsultantsPart_IsRecordedAsOnTheirBehalf()
    {
        var invoice = GivenInvoice();
        var request = GivenPendingRequest(invoice, 100m);

        var result = await _sut.ApproveAsync(ConsultantPart(request, DrKarthikeyan).Id, new ApproveInvoiceDiscountRequest(), SuperAdmin, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Approvals.Single().DecidedOnBehalf.Should().BeTrue();
        result.Value.Status.Should().Be(InvoiceDiscountRequestStatus.Approved);
        invoice.DiscountStatus.Should().Be(InvoiceDiscountRequestStatus.Approved);
    }

    [Fact]
    public async Task ApproveAsync_ByAnotherConsultant_IsRefused()
    {
        var invoice = GivenInvoice();
        var request = GivenPendingRequest(invoice, 100m);

        var result = await _sut.ApproveAsync(ConsultantPart(request, DrKarthikeyan).Id, new ApproveInvoiceDiscountRequest(), Parimalam, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.DiscountNotYourApproval);
        invoice.Items.First().Discount.Should().Be(0m);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveAsync_ByAConsultantOnTheSuperAdminsPart_IsRefused()
    {
        var invoice = GivenInvoice();
        var request = GivenPendingRequest(invoice, 100m, 50m);

        var result = await _sut.ApproveAsync(SuperAdminPart(request).Id, new ApproveInvoiceDiscountRequest(), Karthikeyan, CancellationToken.None);

        result.ErrorCode.Should().Be(BillingErrorCodes.DiscountNotYourApproval);
    }

    [Fact]
    public async Task ApproveAsync_ByAReceptionist_IsRefused()
    {
        var invoice = GivenInvoice();
        var request = GivenPendingRequest(invoice, 0m, 50m);

        var result = await _sut.ApproveAsync(SuperAdminPart(request).Id, new ApproveInvoiceDiscountRequest(), Receptionist, CancellationToken.None);

        result.ErrorCode.Should().Be(BillingErrorCodes.DiscountNotYourApproval);
    }

    [Fact]
    public async Task ApproveAsync_OnADecidedPart_IsAConflict()
    {
        var invoice = GivenInvoice();
        var request = GivenPendingRequest(invoice, 0m, 50m);
        request.RejectPart(SuperAdminPart(request).Id, null, "superadmin", "No", onBehalf: false);

        var result = await _sut.ApproveAsync(SuperAdminPart(request).Id, new ApproveInvoiceDiscountRequest(), SuperAdmin, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.DiscountRequestNotPending);
    }

    [Fact]
    public async Task ApproveAsync_OnAVoidedBill_IsRefused()
    {
        var invoice = GivenInvoice();
        var request = GivenPendingRequest(invoice, 0m, 50m);
        invoice.Void("Patient left", null);

        var result = await _sut.ApproveAsync(SuperAdminPart(request).Id, new ApproveInvoiceDiscountRequest(), SuperAdmin, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.AlreadyVoided);
    }

    [Fact]
    public async Task ApproveAsync_WhenTheLineWasPaidMeanwhile_IsRefusedAndLeavesThePaidLineAlone()
    {
        var invoice = GivenInvoice();
        var request = GivenPendingRequest(invoice, 100m);
        invoice.MarkItemPaid(invoice.Items.First().Id, null);

        var result = await _sut.ApproveAsync(ConsultantPart(request, DrKarthikeyan).Id, new ApproveInvoiceDiscountRequest(), Karthikeyan, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.DiscountNotEligible);
        invoice.Items.First().Discount.Should().Be(0m);
    }

    [Fact]
    public async Task ApproveAsync_OnAnUnknownPart_IsNotFound()
    {
        var result = await _sut.ApproveAsync(Guid.NewGuid(), new ApproveInvoiceDiscountRequest(), SuperAdmin, CancellationToken.None);

        result.ErrorCode.Should().Be(BillingErrorCodes.DiscountRequestNotFound);
    }

    [Fact]
    public async Task RejectAsync_OfTheOnlyPart_KeepsTheBillsPriceAndMakesItPayableAgain()
    {
        var invoice = GivenInvoice();
        var request = GivenPendingRequest(invoice, 0m, 50m);

        var result = await _sut.RejectAsync(SuperAdminPart(request).Id, new RejectInvoiceDiscountRequest { Notes = "Not eligible" }, SuperAdmin, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(InvoiceDiscountRequestStatus.Rejected);
        result.Value.DecisionNotes.Should().Be("Not eligible");
        result.Value.Approvals.Single().DecisionNotes.Should().Be("Not eligible");
        invoice.NetAmount.Should().Be(800m);
        invoice.DiscountStatus.Should().Be(InvoiceDiscountRequestStatus.Rejected);
    }

    [Fact]
    public async Task RejectAsync_ByTheConsultantWhileTheSuperAdminsPartWaits_KeepsTheBillWaiting()
    {
        var invoice = GivenInvoice();
        var request = GivenPendingRequest(invoice, 100m, 50m);

        var result = await _sut.RejectAsync(ConsultantPart(request, DrKarthikeyan).Id, new RejectInvoiceDiscountRequest { Notes = "Full fee" }, Karthikeyan, CancellationToken.None);

        result.Value!.Status.Should().Be(InvoiceDiscountRequestStatus.Pending);
        invoice.DiscountStatus.Should().Be(InvoiceDiscountRequestStatus.Pending);
    }

    [Fact]
    public async Task ApprovingOnePartAndRejectingTheOther_LeavesTheBillPartlyApproved()
    {
        var invoice = GivenInvoice();
        var request = GivenPendingRequest(invoice, 100m, 50m);
        await _sut.RejectAsync(ConsultantPart(request, DrKarthikeyan).Id, new RejectInvoiceDiscountRequest { Notes = "Full fee" }, Karthikeyan, CancellationToken.None);

        var result = await _sut.ApproveAsync(SuperAdminPart(request).Id, new ApproveInvoiceDiscountRequest(), SuperAdmin, CancellationToken.None);

        result.Value!.Status.Should().Be(InvoiceDiscountRequestStatus.PartlyApproved);
        result.Value.ApprovedDiscount.Should().Be(50m);
        invoice.Items.First().Total.Should().Be(500m);
        invoice.Items.Last().Total.Should().Be(250m);
        invoice.NetAmount.Should().Be(750m);
        invoice.DiscountStatus.Should().Be(InvoiceDiscountRequestStatus.PartlyApproved);
    }

    [Fact]
    public async Task RejectAsync_ByAnotherConsultant_IsRefused()
    {
        var invoice = GivenInvoice();
        var request = GivenPendingRequest(invoice, 100m);

        var result = await _sut.RejectAsync(ConsultantPart(request, DrKarthikeyan).Id, new RejectInvoiceDiscountRequest { Notes = "No" }, Parimalam, CancellationToken.None);

        result.ErrorCode.Should().Be(BillingErrorCodes.DiscountNotYourApproval);
        request.Status.Should().Be(InvoiceDiscountRequestStatus.Pending);
    }

    [Fact]
    public async Task RejectAsync_OnAnUnknownPart_IsNotFound()
    {
        var result = await _sut.RejectAsync(Guid.NewGuid(), new RejectInvoiceDiscountRequest { Notes = "No" }, SuperAdmin, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.DiscountRequestNotFound);
    }

    [Fact]
    public async Task GetPagedForConsultantAsync_AsksTheRepositoryForThatConsultantsRequests()
    {
        var invoice = GivenInvoice();
        var request = GivenPendingRequest(invoice, 100m);
        var query = new InvoiceDiscountRequestListQuery { Status = InvoiceDiscountRequestStatus.Pending, PageSize = 500 };
        _repository.GetPagedForConsultantAsync(DrKarthikeyan, Arg.Any<InvoiceDiscountRequestListQuery>(), Arg.Any<CancellationToken>())
            .Returns(([request], 1));

        var result = await _sut.GetPagedForConsultantAsync(DrKarthikeyan, query, CancellationToken.None);

        result.Items.Should().ContainSingle().Which.Id.Should().Be(request.Id);
        await _repository.Received(1).GetPagedForConsultantAsync(DrKarthikeyan, Arg.Is<InvoiceDiscountRequestListQuery>(q => q.PageSize == 100 && q.Status == InvoiceDiscountRequestStatus.Pending), Arg.Any<CancellationToken>());
    }
}
