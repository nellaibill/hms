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
    private static readonly BillingActor Receptionist = new(Guid.NewGuid(), "reception1", IsSuperAdmin: false);
    private static readonly BillingActor SuperAdmin = new(Guid.NewGuid(), "superadmin", IsSuperAdmin: true);
    private readonly Guid _approverId = Guid.NewGuid();

    public InvoiceDiscountServiceTests()
    {
        _sut = new InvoiceDiscountService(_invoiceRepository, _repository, Substitute.For<IActivityLogService>());
        _repository.GetByInvoiceIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    private static InvoiceLineItemSpec Spec(BillingType billingType = BillingType.Consultation, decimal unitPrice = 500m, decimal discount = 0m) =>
        new(billingType, null, null, null, null, 1, unitPrice, discount, false, null);

    /// <summary>An unpaid ₹500 consultation + ₹300 lab bill, unless other lines are given.</summary>
    private Invoice GivenInvoice(params InvoiceLineItemSpec[] items)
    {
        var invoice = Invoice.Create("INV-2026-000200", Guid.NewGuid(), Guid.NewGuid(), "Aravind Nadar", "NH20260001",
            items.Length > 0 ? items : [Spec(), Spec(BillingType.Laboratory, 300m)], createdBy: null);
        _invoiceRepository.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        return invoice;
    }

    private static CreateInvoiceDiscountRequest RequestFor(Invoice invoice, params decimal[] discounts) => new()
    {
        DiscountType = InvoiceDiscountType.Amount,
        DiscountValue = discounts.Sum(),
        Reason = "  Staff family  ",
        Lines = invoice.Items.Zip(discounts, (line, discount) => new InvoiceDiscountLineRequest { LineItemId = line.Id, Discount = discount }).ToList(),
    };

    private InvoiceDiscountRequest GivenPendingRequest(Invoice invoice, decimal discount = 100m)
    {
        var request = InvoiceDiscountRequest.Create(invoice, [(invoice.Items.First(), discount)], InvoiceDiscountType.Amount, discount, "Staff family", "reception1", Guid.NewGuid());
        invoice.MarkDiscountPending(null);
        _repository.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        return request;
    }

    [Fact]
    public async Task CreateAsync_FromAReceptionist_RaisesAPendingRequestAndLeavesTheBillAtFullPrice()
    {
        var invoice = GivenInvoice();

        var result = await _sut.CreateAsync(invoice.Id, RequestFor(invoice, 50m, 30m), Receptionist, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(InvoiceDiscountRequestStatus.Pending);
        result.Value.Reason.Should().Be("Staff family");
        result.Value.TotalDiscount.Should().Be(80m);
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
    public async Task CreateAsync_FromASuperAdmin_AppliesTheDiscountAtOnce()
    {
        var invoice = GivenInvoice();

        var result = await _sut.CreateAsync(invoice.Id, RequestFor(invoice, 50m), SuperAdmin, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(InvoiceDiscountRequestStatus.Approved);
        result.Value.DecidedByName.Should().Be("superadmin");
        invoice.NetAmount.Should().Be(750m);
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
        var pending = GivenPendingRequest(invoice);
        _repository.GetByInvoiceIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns([pending]);

        var result = await _sut.GetForInvoiceAsync(invoice.Id, CancellationToken.None);

        result.Value!.CanRequest.Should().BeFalse();
        result.Value.IneligibleReason.Should().Contain("already waiting");
        result.Value.Requests.Should().ContainSingle().Which.Id.Should().Be(pending.Id);
    }

    [Fact]
    public async Task ApproveAsync_AppliesTheDiscountToTheBillAndRecordsTheApprover()
    {
        var invoice = GivenInvoice();
        var request = GivenPendingRequest(invoice, 100m);

        var result = await _sut.ApproveAsync(request.Id, new ApproveInvoiceDiscountRequest { Notes = "OK" }, _approverId, "superadmin", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(InvoiceDiscountRequestStatus.Approved);
        result.Value.DecidedBy.Should().Be(_approverId);
        result.Value.DecisionNotes.Should().Be("OK");
        invoice.NetAmount.Should().Be(700m);
        invoice.TotalDiscount.Should().Be(100m);
        invoice.DiscountStatus.Should().Be(InvoiceDiscountRequestStatus.Approved);
        var line = invoice.Items.First();
        line.Total.Should().Be(400m);
        line.DiscountApproved.Should().BeTrue();
        line.DiscountApprovedBy.Should().Be("superadmin");
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveAsync_OnADecidedRequest_IsAConflict()
    {
        var invoice = GivenInvoice();
        var request = GivenPendingRequest(invoice);
        request.Reject(null, "superadmin", "No");

        var result = await _sut.ApproveAsync(request.Id, new ApproveInvoiceDiscountRequest(), _approverId, "superadmin", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.DiscountRequestNotPending);
    }

    [Fact]
    public async Task ApproveAsync_OnAVoidedBill_IsRefused()
    {
        var invoice = GivenInvoice();
        var request = GivenPendingRequest(invoice);
        invoice.Void("Patient left", null);

        var result = await _sut.ApproveAsync(request.Id, new ApproveInvoiceDiscountRequest(), _approverId, "superadmin", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.AlreadyVoided);
    }

    [Fact]
    public async Task ApproveAsync_WhenTheLineWasPaidMeanwhile_IsRefusedAndLeavesThePaidLineAlone()
    {
        var invoice = GivenInvoice();
        var request = GivenPendingRequest(invoice);
        invoice.MarkItemPaid(invoice.Items.First().Id, null);

        var result = await _sut.ApproveAsync(request.Id, new ApproveInvoiceDiscountRequest(), _approverId, "superadmin", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.DiscountNotEligible);
        invoice.Items.First().Discount.Should().Be(0m);
    }

    [Fact]
    public async Task RejectAsync_KeepsTheBillsPriceAndMakesItPayableAgain()
    {
        var invoice = GivenInvoice();
        var request = GivenPendingRequest(invoice);

        var result = await _sut.RejectAsync(request.Id, new RejectInvoiceDiscountRequest { Notes = "Not eligible" }, _approverId, "superadmin", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(InvoiceDiscountRequestStatus.Rejected);
        result.Value.DecisionNotes.Should().Be("Not eligible");
        invoice.NetAmount.Should().Be(800m);
        invoice.DiscountStatus.Should().Be(InvoiceDiscountRequestStatus.Rejected);
    }

    [Fact]
    public async Task RejectAsync_OnAnUnknownRequest_IsNotFound()
    {
        var result = await _sut.RejectAsync(Guid.NewGuid(), new RejectInvoiceDiscountRequest { Notes = "No" }, _approverId, "superadmin", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.DiscountRequestNotFound);
    }
}
