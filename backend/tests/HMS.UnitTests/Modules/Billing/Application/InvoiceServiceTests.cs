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
}
