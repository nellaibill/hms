using FluentAssertions;
using HMS.Modules.ActivityLog.Application;
using HMS.Modules.Billing.Application;
using HMS.Modules.Billing.Application.Abstractions;
using HMS.Modules.Billing.Contracts;
using HMS.Modules.Billing.Domain;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Contracts;
using HMS.Shared.Kernel;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Billing.Application;

public class InvoiceRevertServiceTests
{
    private readonly IInvoiceRepository _invoiceRepository = Substitute.For<IInvoiceRepository>();
    private readonly IInvoiceRevertRequestRepository _repository = Substitute.For<IInvoiceRevertRequestRepository>();
    private readonly IPatientVisitService _patientVisitService = Substitute.For<IPatientVisitService>();
    private readonly InvoiceRevertService _sut;
    private readonly Guid _actorId = Guid.NewGuid();

    public InvoiceRevertServiceTests()
    {
        _sut = new InvoiceRevertService(_invoiceRepository, _repository, _patientVisitService, Substitute.For<IActivityLogService>());
        _repository.GetByInvoiceIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    private static InvoiceLineItemSpec Spec(BillingType billingType = BillingType.Consultation, decimal unitPrice = 250m) =>
        new(billingType, null, null, null, null, 1, unitPrice, 0m, false, null);

    /// <summary>A paid OP consultation bill on a registered OP visit — the common case.</summary>
    private Invoice GivenInvoice(VisitType visitType = VisitType.OP, bool paid = true, params InvoiceLineItemSpec[] items)
    {
        var patientId = Guid.NewGuid();
        var visitId = Guid.NewGuid();
        var invoice = Invoice.Create("INV-2026-000100", patientId, visitId, "Aravind Nadar", "NH20260001", items.Length > 0 ? items : [Spec()], createdBy: null);
        if (paid)
        {
            foreach (var item in invoice.Items.ToList())
            {
                invoice.MarkItemPaid(item.Id, null);
            }
        }

        _invoiceRepository.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        _patientVisitService.GetByIdAsync(patientId, visitId, Arg.Any<CancellationToken>())
            .Returns(Result<PatientVisitResponse>.Success(new PatientVisitResponse { VisitId = visitId, PatientId = patientId, VisitType = visitType }));
        return invoice;
    }

    private InvoiceRevertRequest GivenPendingRequest(Invoice invoice, string reason = "Patient left before consultation")
    {
        var request = InvoiceRevertRequest.Create(invoice, reason, "reception1", Guid.NewGuid());
        _repository.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        return request;
    }

    [Fact]
    public async Task CreateAsync_OnAPaidOpBill_RaisesAPendingRequestWithTheReasonAndPaidAmount()
    {
        var invoice = GivenInvoice();

        var result = await _sut.CreateAsync(invoice.Id, new CreateInvoiceRevertRequest { Reason = "  Billed the wrong consultant  " }, _actorId, "reception1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(InvoiceRevertRequestStatus.Pending);
        result.Value.Reason.Should().Be("Billed the wrong consultant");
        result.Value.PaidAmount.Should().Be(250m);
        result.Value.RequestedBy.Should().Be(_actorId);
        result.Value.RequestedByName.Should().Be("reception1");
        await _repository.Received(1).AddAsync(Arg.Any<InvoiceRevertRequest>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        invoice.IsVoided.Should().BeFalse("the bill only changes once a Super Admin approves");
    }

    [Fact]
    public async Task CreateAsync_OnABillWithNoRegisteredVisit_IsTreatedAsAnOpBill()
    {
        var patientId = Guid.NewGuid();
        var invoice = Invoice.Create("INV-2026-000101", patientId, patientId, "Test Patient", "NH1", [Spec()], createdBy: null);
        _invoiceRepository.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await _sut.CreateAsync(invoice.Id, new CreateInvoiceRevertRequest { Reason = "Duplicate bill" }, _actorId, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _patientVisitService.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(VisitType.IP)]
    [InlineData(VisitType.Emergency)]
    public async Task CreateAsync_OnANonOpVisitsBill_IsNotEligible(VisitType visitType)
    {
        var invoice = GivenInvoice(visitType);

        var result = await _sut.CreateAsync(invoice.Id, new CreateInvoiceRevertRequest { Reason = "Wrong bill" }, _actorId, null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.RevertNotEligible);
        await _repository.DidNotReceive().AddAsync(Arg.Any<InvoiceRevertRequest>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(BillingType.Pharmacy)]
    [InlineData(BillingType.InpatientCharge)]
    public async Task CreateAsync_OnABillWithPharmacyOrInpatientLines_IsNotEligible(BillingType billingType)
    {
        var invoice = GivenInvoice(VisitType.OP, true, Spec(), Spec(billingType, 400m));

        var result = await _sut.CreateAsync(invoice.Id, new CreateInvoiceRevertRequest { Reason = "Wrong bill" }, _actorId, null, CancellationToken.None);

        result.ErrorCode.Should().Be(BillingErrorCodes.RevertNotEligible);
    }

    [Fact]
    public async Task CreateAsync_WhenARequestIsAlreadyPending_ReturnsAlreadyPending()
    {
        var invoice = GivenInvoice();
        var pending = InvoiceRevertRequest.Create(invoice, "First request", "reception1", null);
        _repository.GetByInvoiceIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns([pending]);

        var result = await _sut.CreateAsync(invoice.Id, new CreateInvoiceRevertRequest { Reason = "Second request" }, _actorId, null, CancellationToken.None);

        result.ErrorCode.Should().Be(BillingErrorCodes.RevertAlreadyPending);
    }

    [Fact]
    public async Task CreateAsync_OnAnAlreadyVoidedBill_IsNotEligible()
    {
        var invoice = GivenInvoice(paid: false);
        invoice.Void("Entered twice", null);

        var result = await _sut.CreateAsync(invoice.Id, new CreateInvoiceRevertRequest { Reason = "Wrong bill" }, _actorId, null, CancellationToken.None);

        result.ErrorCode.Should().Be(BillingErrorCodes.RevertNotEligible);
    }

    [Fact]
    public async Task GetForInvoiceAsync_ReportsWhetherARevertCanBeRequested()
    {
        var invoice = GivenInvoice();
        var rejected = InvoiceRevertRequest.Create(invoice, "First try", "reception1", null);
        rejected.Reject(Guid.NewGuid(), "superadmin", "Consultation was given");
        _repository.GetByInvoiceIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns([rejected]);

        var result = await _sut.GetForInvoiceAsync(invoice.Id, CancellationToken.None);

        result.Value!.CanRequest.Should().BeTrue("a rejected request doesn't block a new one");
        result.Value.Requests.Should().ContainSingle().Which.DecisionNotes.Should().Be("Consultation was given");
    }

    [Fact]
    public async Task ApproveAsync_RevertsThePaidBillAndRecordsTheApprover()
    {
        var invoice = GivenInvoice();
        var request = GivenPendingRequest(invoice);

        var result = await _sut.ApproveAsync(request.Id, new ApproveInvoiceRevertRequest { Notes = "Refund in cash" }, _actorId, "superadmin", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(InvoiceRevertRequestStatus.Approved);
        result.Value.DecidedBy.Should().Be(_actorId);
        result.Value.DecidedByName.Should().Be("superadmin");
        result.Value.DecisionNotes.Should().Be("Refund in cash");
        invoice.IsVoided.Should().BeTrue();
        invoice.IsReverted.Should().BeTrue();
        invoice.VoidReason.Should().Be("Patient left before consultation");
        invoice.VoidedBy.Should().Be(_actorId);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveAsync_OnADecidedRequest_ReturnsNotPendingAndLeavesTheBillAlone()
    {
        var invoice = GivenInvoice();
        var request = GivenPendingRequest(invoice);
        request.Reject(null, "superadmin", "No");

        var result = await _sut.ApproveAsync(request.Id, new ApproveInvoiceRevertRequest(), _actorId, "superadmin", CancellationToken.None);

        result.ErrorCode.Should().Be(BillingErrorCodes.RevertRequestNotPending);
        invoice.IsVoided.Should().BeFalse();
    }

    [Fact]
    public async Task ApproveAsync_WhenTheBillWasVoidedMeanwhile_ReturnsAlreadyVoided()
    {
        var invoice = GivenInvoice(paid: false);
        var request = GivenPendingRequest(invoice);
        invoice.Void("Voided at the counter", null);

        var result = await _sut.ApproveAsync(request.Id, new ApproveInvoiceRevertRequest(), _actorId, "superadmin", CancellationToken.None);

        result.ErrorCode.Should().Be(BillingErrorCodes.AlreadyVoided);
        request.Status.Should().Be(InvoiceRevertRequestStatus.Pending);
    }

    [Fact]
    public async Task ApproveAsync_WithAnUnknownId_ReturnsNotFound()
    {
        var result = await _sut.ApproveAsync(Guid.NewGuid(), new ApproveInvoiceRevertRequest(), _actorId, "superadmin", CancellationToken.None);

        result.ErrorCode.Should().Be(BillingErrorCodes.RevertRequestNotFound);
    }

    [Fact]
    public async Task RejectAsync_RecordsTheReasonAndLeavesTheBillUnchanged()
    {
        var invoice = GivenInvoice();
        var request = GivenPendingRequest(invoice);

        var result = await _sut.RejectAsync(request.Id, new RejectInvoiceRevertRequest { Notes = "Consultation was given" }, _actorId, "superadmin", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(InvoiceRevertRequestStatus.Rejected);
        result.Value.DecisionNotes.Should().Be("Consultation was given");
        invoice.IsVoided.Should().BeFalse();
    }

    [Fact]
    public async Task GetPagedAsync_ClampsThePageSize()
    {
        _repository.GetPagedAsync(Arg.Any<InvoiceRevertRequestListQuery>(), Arg.Any<CancellationToken>()).Returns(([], 0));

        await _sut.GetPagedAsync(new InvoiceRevertRequestListQuery { Page = 0, PageSize = 1000 }, CancellationToken.None);

        await _repository.Received(1).GetPagedAsync(Arg.Is<InvoiceRevertRequestListQuery>(q => q.Page == 1 && q.PageSize == 100), Arg.Any<CancellationToken>());
    }
}
