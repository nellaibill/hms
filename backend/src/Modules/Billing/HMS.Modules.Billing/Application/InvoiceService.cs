using HMS.Modules.Billing.Application.Abstractions;
using HMS.Modules.Billing.Application.Mapping;
using HMS.Modules.Billing.Contracts;
using HMS.Modules.Billing.Domain;
using HMS.Modules.Patients.Application;
using HMS.Shared.Kernel;

namespace HMS.Modules.Billing.Application;

/// <summary>
/// Public (not internal): InvoicesController — which ASP.NET Core requires to be a public
/// class with a public constructor for controller discovery/DI activation — takes this as
/// a constructor dependency; a public constructor cannot have an internal parameter type
/// (CS0051).
/// </summary>
public interface IInvoiceService
{
    Task<Result<InvoiceResponse>> CreateAsync(CreateInvoiceRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<InvoiceResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<InvoiceResponse>> GetPagedAsync(InvoiceListQuery query, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<InvoiceResponse>>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken);

    Task<Result<InvoiceResponse>> RecordPaymentAsync(Guid invoiceId, Guid itemId, RecordPaymentRequest request, Guid? actorId, CancellationToken cancellationToken);
}

internal class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _repository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IInvoiceNumberGenerator _numberGenerator;
    private readonly IPatientService _patientService;

    public InvoiceService(
        IInvoiceRepository repository,
        IPaymentRepository paymentRepository,
        IInvoiceNumberGenerator numberGenerator,
        IPatientService patientService)
    {
        _repository = repository;
        _paymentRepository = paymentRepository;
        _numberGenerator = numberGenerator;
        _patientService = patientService;
    }

    public async Task<Result<InvoiceResponse>> CreateAsync(CreateInvoiceRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
        {
            return Result<InvoiceResponse>.Failure(BillingErrorCodes.EmptyInvoice, "At least one billing item is required.");
        }

        if (!(await _patientService.GetByIdAsync(request.PatientId, cancellationToken)).IsSuccess)
        {
            return Result<InvoiceResponse>.Failure(BillingErrorCodes.InvalidPatient, $"Patient '{request.PatientId}' was not found.");
        }

        var invoiceNumber = await _numberGenerator.NextInvoiceNumberAsync(cancellationToken);

        var itemSpecs = request.Items
            .Select(i => new InvoiceLineItemSpec(
                i.BillingType,
                i.DepartmentId,
                i.ConsultantId,
                i.ServiceId,
                i.Quantity,
                i.UnitPrice,
                i.Discount,
                i.DiscountApproved,
                i.DiscountApprovedBy))
            .ToList();

        var invoice = Invoice.Create(
            invoiceNumber,
            request.PatientId,
            request.VisitId,
            request.PatientName,
            request.PatientUhid,
            itemSpecs,
            actorId);

        await _repository.AddAsync(invoice, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<InvoiceResponse>.Success(invoice.ToResponse());
    }

    public async Task<Result<InvoiceResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await _repository.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
        {
            return Result<InvoiceResponse>.Failure(BillingErrorCodes.NotFound, $"Invoice '{id}' was not found.");
        }

        return Result<InvoiceResponse>.Success(invoice.ToResponse());
    }

    public async Task<PagedResult<InvoiceResponse>> GetPagedAsync(InvoiceListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<InvoiceResponse>(items.Select(i => i.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result<IReadOnlyList<InvoiceResponse>>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken)
    {
        if (!(await _patientService.GetByIdAsync(patientId, cancellationToken)).IsSuccess)
        {
            return Result<IReadOnlyList<InvoiceResponse>>.Failure(BillingErrorCodes.InvalidPatient, $"Patient '{patientId}' was not found.");
        }

        var invoices = await _repository.GetByPatientIdAsync(patientId, cancellationToken);
        return Result<IReadOnlyList<InvoiceResponse>>.Success(invoices.Select(i => i.ToResponse()).ToList());
    }

    public async Task<Result<InvoiceResponse>> RecordPaymentAsync(Guid invoiceId, Guid itemId, RecordPaymentRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var invoice = await _repository.GetByIdAsync(invoiceId, cancellationToken);
        if (invoice is null)
        {
            return Result<InvoiceResponse>.Failure(BillingErrorCodes.NotFound, $"Invoice '{invoiceId}' was not found.");
        }

        var item = invoice.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return Result<InvoiceResponse>.Failure(BillingErrorCodes.LineItemNotFound, $"Line item '{itemId}' was not found on invoice '{invoiceId}'.");
        }

        if (item.PaymentStatus == Contracts.PaymentStatus.Paid)
        {
            return Result<InvoiceResponse>.Failure(BillingErrorCodes.LineItemAlreadyPaid, $"Line item '{itemId}' is already paid.");
        }

        var paidItem = invoice.MarkItemPaid(itemId, actorId);
        var payment = Payment.Create(invoice.Id, paidItem.Id, paidItem.Total, request.Method, actorId);

        await _paymentRepository.AddAsync(payment, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<InvoiceResponse>.Success(invoice.ToResponse());
    }
}
