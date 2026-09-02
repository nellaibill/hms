using FluentAssertions;
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

public class InvoiceServiceTests
{
    private readonly IInvoiceRepository _repository = Substitute.For<IInvoiceRepository>();
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly IInvoiceNumberGenerator _numberGenerator = Substitute.For<IInvoiceNumberGenerator>();
    private readonly IPatientService _patientService = Substitute.For<IPatientService>();
    private readonly InvoiceService _sut;
    private readonly Guid _patientId = Guid.NewGuid();

    public InvoiceServiceTests()
    {
        _sut = new InvoiceService(_repository, _paymentRepository, _numberGenerator, _patientService);

        _patientService.GetByIdAsync(_patientId, Arg.Any<CancellationToken>())
            .Returns(Result<PatientResponse>.Success(new PatientResponse()));
        _numberGenerator.NextInvoiceNumberAsync(Arg.Any<CancellationToken>()).Returns("INV-2026-000001");
    }

    private CreateInvoiceRequest ValidRequest() => new()
    {
        PatientId = _patientId,
        VisitId = Guid.NewGuid(),
        PatientName = "Aravind Nadar",
        PatientUhid = "NH20260001",
        Items =
        [
            new CreateInvoiceLineItemRequest { BillingType = BillingType.Consultation, DepartmentId = "cardiology", ConsultantId = "dr-revathi", Quantity = 1, UnitPrice = 720m },
        ],
    };

    [Fact]
    public async Task CreateAsync_WithValidRequest_SavesInvoiceAndReturnsSuccess()
    {
        var result = await _sut.CreateAsync(ValidRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.InvoiceNumber.Should().Be("INV-2026-000001");
        result.Value.NetAmount.Should().Be(720m);
        result.Value.Items.Single().DepartmentId.Should().Be("cardiology");
        await _repository.Received(1).AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithNoItems_ReturnsEmptyInvoiceFailure()
    {
        var request = ValidRequest() with { Items = [] };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.EmptyInvoice);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithNoPayment_LeavesEveryItemPending()
    {
        var result = await _sut.CreateAsync(ValidRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PaymentStatus.Should().Be(PaymentStatus.Pending);
        result.Value.Items.Single().PaymentStatus.Should().Be(PaymentStatus.Pending);
        await _paymentRepository.DidNotReceive().AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithPayment_MarksEveryItemPaidAndPostsOnePaymentPerItem()
    {
        var request = ValidRequest() with
        {
            Items =
            [
                new CreateInvoiceLineItemRequest { BillingType = BillingType.Consultation, DepartmentId = "cardiology", ConsultantId = "dr-revathi", Quantity = 1, UnitPrice = 720m },
                new CreateInvoiceLineItemRequest { BillingType = BillingType.Laboratory, ConsultantId = "dr-revathi", ServiceId = "svc-cbc", Quantity = 1, UnitPrice = 300m },
            ],
            Payments = [new CreateInvoicePaymentRequest { Method = PaymentMethod.Upi, Amount = 1020m, ReferenceNumber = "UPI-REF-12345" }],
        };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PaymentStatus.Should().Be(PaymentStatus.Paid);
        result.Value.Items.Should().OnlyContain(i => i.PaymentStatus == PaymentStatus.Paid);
        await _paymentRepository.Received(2).AddAsync(
            Arg.Is<Payment>(p => p.Method == PaymentMethod.Upi && p.ReferenceNumber == "UPI-REF-12345"),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithOvertenderedSinglePayment_MarksEveryItemPaidAndIgnoresChange()
    {
        // 1200 tendered in cash against a 1020 bill — the 180 "change" is a front-desk
        // calculation only and must never be persisted as part of either item's Payment.
        var request = ValidRequest() with
        {
            Items =
            [
                new CreateInvoiceLineItemRequest { BillingType = BillingType.Consultation, DepartmentId = "cardiology", ConsultantId = "dr-revathi", Quantity = 1, UnitPrice = 720m },
                new CreateInvoiceLineItemRequest { BillingType = BillingType.Laboratory, ConsultantId = "dr-revathi", ServiceId = "svc-cbc", Quantity = 1, UnitPrice = 300m },
            ],
            Payments = [new CreateInvoicePaymentRequest { Method = PaymentMethod.Cash, Amount = 1200m }],
        };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PaymentStatus.Should().Be(PaymentStatus.Paid);
        await _paymentRepository.Received(1).AddAsync(Arg.Is<Payment>(p => p.Amount == 720m), Arg.Any<CancellationToken>());
        await _paymentRepository.Received(1).AddAsync(Arg.Is<Payment>(p => p.Amount == 300m), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithSplitPaymentMatchingTotal_PostsOnePaymentPerMethodPerItemSplit()
    {
        // 1020 total, split 300 Upi + 720 Cash — the Upi row exactly covers the first item and
        // the Cash row exactly covers the second, so this is still one Payment per item, but a
        // 1:1 method-to-item split isn't guaranteed in general (see the next test for a split
        // that crosses an item boundary). Non-Cash rows are always applied before Cash ones
        // regardless of the order they're listed in the request (see InvoiceService.CreateAsync's
        // own comment on why), which is why this item order matches Upi-then-Cash rather than
        // the payments list's own order.
        var request = ValidRequest() with
        {
            Items =
            [
                new CreateInvoiceLineItemRequest { BillingType = BillingType.Consultation, DepartmentId = "cardiology", ConsultantId = "dr-revathi", Quantity = 1, UnitPrice = 300m },
                new CreateInvoiceLineItemRequest { BillingType = BillingType.Laboratory, ConsultantId = "dr-revathi", ServiceId = "svc-cbc", Quantity = 1, UnitPrice = 720m },
            ],
            Payments =
            [
                new CreateInvoicePaymentRequest { Method = PaymentMethod.Cash, Amount = 720m },
                new CreateInvoicePaymentRequest { Method = PaymentMethod.Upi, Amount = 300m, ReferenceNumber = "UPI-REF-99" },
            ],
        };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PaymentStatus.Should().Be(PaymentStatus.Paid);
        await _paymentRepository.Received(2).AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
        await _paymentRepository.Received(1).AddAsync(Arg.Is<Payment>(p => p.Method == PaymentMethod.Upi && p.Amount == 300m && p.ReferenceNumber == "UPI-REF-99"), Arg.Any<CancellationToken>());
        await _paymentRepository.Received(1).AddAsync(Arg.Is<Payment>(p => p.Method == PaymentMethod.Cash && p.Amount == 720m), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithSplitPaymentCrossingAnItemBoundary_AllocatesEachRowAcrossItemsAsNeeded()
    {
        // 1000 total across 2 items (600 + 400), split 300 Upi + 700 Cash — neither row lines up
        // with an item boundary, so the waterfall must produce: item1 gets 300 from Upi plus 300
        // from Cash; item2 gets the rest of Cash (400). Upi always applies before Cash (see
        // InvoiceService.CreateAsync), so this is Upi-first regardless of the payments list order.
        var request = ValidRequest() with
        {
            Items =
            [
                new CreateInvoiceLineItemRequest { BillingType = BillingType.Consultation, DepartmentId = "cardiology", ConsultantId = "dr-revathi", Quantity = 1, UnitPrice = 600m },
                new CreateInvoiceLineItemRequest { BillingType = BillingType.Laboratory, ConsultantId = "dr-revathi", ServiceId = "svc-cbc", Quantity = 1, UnitPrice = 400m },
            ],
            Payments =
            [
                new CreateInvoicePaymentRequest { Method = PaymentMethod.Cash, Amount = 700m },
                new CreateInvoicePaymentRequest { Method = PaymentMethod.Upi, Amount = 300m },
            ],
        };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PaymentStatus.Should().Be(PaymentStatus.Paid);
        await _paymentRepository.Received(3).AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
        await _paymentRepository.Received(1).AddAsync(Arg.Is<Payment>(p => p.Method == PaymentMethod.Upi && p.Amount == 300m), Arg.Any<CancellationToken>());
        await _paymentRepository.Received(1).AddAsync(Arg.Is<Payment>(p => p.Method == PaymentMethod.Cash && p.Amount == 300m), Arg.Any<CancellationToken>());
        await _paymentRepository.Received(1).AddAsync(Arg.Is<Payment>(p => p.Method == PaymentMethod.Cash && p.Amount == 400m), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithSplitPaymentOvertenderedInCash_TreatsTheExcessAsChangeFromCash()
    {
        // The real-world case this exists for: a 700 bill paid as 570 Upi + 200 Cash — 770
        // tendered, 70 over. Since only Cash can realistically hand back change at the counter,
        // the 70 must come out of the Cash row (130 of it applied, not the full 200), never out
        // of Upi — confirmed by asserting the Upi Payment is for the full 570 tendered, and Cash
        // for exactly 130 (not 200), with nothing persisted for the 70 unconsumed remainder.
        var request = ValidRequest() with
        {
            Items = [new CreateInvoiceLineItemRequest { BillingType = BillingType.Consultation, DepartmentId = "cardiology", ConsultantId = "dr-revathi", Quantity = 1, UnitPrice = 700m }],
            Payments =
            [
                new CreateInvoicePaymentRequest { Method = PaymentMethod.Upi, Amount = 570m },
                new CreateInvoicePaymentRequest { Method = PaymentMethod.Cash, Amount = 200m },
            ],
        };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PaymentStatus.Should().Be(PaymentStatus.Paid);
        await _paymentRepository.Received(2).AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
        await _paymentRepository.Received(1).AddAsync(Arg.Is<Payment>(p => p.Method == PaymentMethod.Upi && p.Amount == 570m), Arg.Any<CancellationToken>());
        await _paymentRepository.Received(1).AddAsync(Arg.Is<Payment>(p => p.Method == PaymentMethod.Cash && p.Amount == 130m), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithSplitPaymentNotAddingUpToTotal_ReturnsPaymentAmountMismatchFailure()
    {
        var request = ValidRequest() with
        {
            Payments =
            [
                new CreateInvoicePaymentRequest { Method = PaymentMethod.Cash, Amount = 500m },
                new CreateInvoicePaymentRequest { Method = PaymentMethod.Upi, Amount = 100m },
            ],
        };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.PaymentAmountMismatch);
        await _paymentRepository.DidNotReceive().AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithSingleUnderpaidPayment_ReturnsPaymentAmountMismatchFailure()
    {
        var request = ValidRequest() with
        {
            Payments = [new CreateInvoicePaymentRequest { Method = PaymentMethod.Cash, Amount = 500m }],
        };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.PaymentAmountMismatch);
        await _paymentRepository.DidNotReceive().AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithNonCashRowsAloneExceedingTotal_ReturnsPaymentAmountMismatchFailure()
    {
        // 700 bill, Upi 750 + Cash 100 — the Upi row alone already covers more than the whole
        // bill, and Upi can't hand back the 50 it's over by the way Cash could, so this must be
        // rejected even though the combined total (850) is more than enough.
        var request = ValidRequest() with
        {
            Items = [new CreateInvoiceLineItemRequest { BillingType = BillingType.Consultation, DepartmentId = "cardiology", ConsultantId = "dr-revathi", Quantity = 1, UnitPrice = 700m }],
            Payments =
            [
                new CreateInvoicePaymentRequest { Method = PaymentMethod.Upi, Amount = 750m },
                new CreateInvoicePaymentRequest { Method = PaymentMethod.Cash, Amount = 100m },
            ],
        };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.PaymentAmountMismatch);
        await _paymentRepository.DidNotReceive().AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenPatientDoesNotExist_ReturnsInvalidPatientFailure()
    {
        _patientService.GetByIdAsync(_patientId, Arg.Any<CancellationToken>())
            .Returns(Result<PatientResponse>.Failure("PATIENT.NOT_FOUND", "not found"));

        var result = await _sut.CreateAsync(ValidRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.InvalidPatient);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordPaymentAsync_WithValidLineItem_MarksItPaidAndPostsPayment()
    {
        var invoice = Invoice.Create(
            "INV-2026-000001",
            _patientId,
            Guid.NewGuid(),
            "Aravind Nadar",
            "NH20260001",
            [new InvoiceLineItemSpec(BillingType.Consultation, null, null, null, null, 1, 720m, 0m, false, null)],
            createdBy: null);
        var itemId = invoice.Items.Single().Id;
        _repository.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await _sut.RecordPaymentAsync(invoice.Id, itemId, new RecordPaymentRequest { Method = PaymentMethod.Cash }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PaymentStatus.Should().Be(PaymentStatus.Paid);
        await _paymentRepository.Received(1).AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordPaymentAsync_WhenInvoiceDoesNotExist_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Invoice?)null);

        var result = await _sut.RecordPaymentAsync(Guid.NewGuid(), Guid.NewGuid(), new RecordPaymentRequest { Method = PaymentMethod.Cash }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.NotFound);
    }

    [Fact]
    public async Task RecordPaymentAsync_WhenLineItemAlreadyPaid_ReturnsConflictFailure()
    {
        var invoice = Invoice.Create(
            "INV-2026-000001",
            _patientId,
            Guid.NewGuid(),
            "Aravind Nadar",
            "NH20260001",
            [new InvoiceLineItemSpec(BillingType.Consultation, null, null, null, null, 1, 720m, 0m, false, null)],
            createdBy: null);
        var itemId = invoice.Items.Single().Id;
        invoice.MarkItemPaid(itemId, updatedBy: null);
        _repository.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await _sut.RecordPaymentAsync(invoice.Id, itemId, new RecordPaymentRequest { Method = PaymentMethod.Cash }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.LineItemAlreadyPaid);
        await _paymentRepository.DidNotReceive().AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordPaymentAsync_WhenInvoiceIsVoided_ReturnsInvoiceVoidedFailure()
    {
        var invoice = Invoice.Create(
            "INV-2026-000001",
            _patientId,
            Guid.NewGuid(),
            "Aravind Nadar",
            "NH20260001",
            [new InvoiceLineItemSpec(BillingType.Consultation, null, null, null, null, 1, 720m, 0m, false, null)],
            createdBy: null);
        var itemId = invoice.Items.Single().Id;
        invoice.Void("Billed in error", voidedBy: null);
        _repository.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await _sut.RecordPaymentAsync(invoice.Id, itemId, new RecordPaymentRequest { Method = PaymentMethod.Cash }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(BillingErrorCodes.InvoiceVoided);
        await _paymentRepository.DidNotReceive().AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
