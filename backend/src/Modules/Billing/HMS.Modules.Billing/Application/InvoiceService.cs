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

    Task<Result<InvoiceResponse>> VoidAsync(Guid id, VoidInvoiceRequest request, Guid? actorId, CancellationToken cancellationToken);
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
                i.PackageId,
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

        // Optional pay-in-full-at-creation: mark every line item Paid and record Payment rows
        // in the same save as the invoice itself, so there's no window where the invoice
        // exists with some lines paid and others not from a partial failure.
        if (request.Payments is { Count: > 0 } payments)
        {
            var totalTendered = payments.Sum(p => p.Amount);

            // Splitting across more than one method must land exactly on the total — there's
            // no single method left to hand change back to. A single method may still
            // overtender (cash change), same as before this field supported more than one row.
            if (payments.Count > 1 && totalTendered != invoice.NetAmount)
            {
                return Result<InvoiceResponse>.Failure(
                    BillingErrorCodes.PaymentAmountMismatch,
                    $"Split payments must add up to exactly the invoice's net amount ({invoice.NetAmount:C}) — there's no change when paying with more than one method.");
            }
            if (payments.Count == 1 && totalTendered < invoice.NetAmount)
            {
                return Result<InvoiceResponse>.Failure(
                    BillingErrorCodes.PaymentAmountMismatch,
                    $"The amount received ({totalTendered:C}) is less than the invoice's net amount ({invoice.NetAmount:C}).");
            }

            // Waterfall: apply each row's tendered amount against line items in order. Payment
            // still records per line item (see Domain/Payment.cs), so a row here may end up
            // split across more than one item, and an item may end up covered by more than one
            // row — the validation above guarantees every item's Total is fully covered by the
            // time this finishes (any leftover on the last row, only possible with a single
            // overtendered row, is genuine change and is never persisted).
            var remaining = payments.Select(p => (p.Method, p.ReferenceNumber, Amount: p.Amount)).ToList();
            var payIndex = 0;
            foreach (var item in invoice.Items)
            {
                var owed = item.Total;
                while (owed > 0 && payIndex < remaining.Count)
                {
                    var (method, referenceNumber, available) = remaining[payIndex];
                    var applied = Math.Min(owed, available);
                    if (applied > 0)
                    {
                        await _paymentRepository.AddAsync(Payment.Create(invoice.Id, item.Id, applied, method, referenceNumber, actorId), cancellationToken);
                        owed -= applied;
                        remaining[payIndex] = (method, referenceNumber, available - applied);
                    }
                    if (remaining[payIndex].Amount <= 0)
                    {
                        payIndex++;
                    }
                }
                invoice.MarkItemPaid(item.Id, actorId);
            }
        }

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
        var payment = Payment.Create(invoice.Id, paidItem.Id, paidItem.Total, request.Method, null, actorId);

        await _paymentRepository.AddAsync(payment, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<InvoiceResponse>.Success(invoice.ToResponse());
    }

    public async Task<Result<InvoiceResponse>> VoidAsync(Guid id, VoidInvoiceRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var invoice = await _repository.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
        {
            return Result<InvoiceResponse>.Failure(BillingErrorCodes.NotFound, $"Invoice '{id}' was not found.");
        }

        if (invoice.IsVoided)
        {
            return Result<InvoiceResponse>.Failure(BillingErrorCodes.AlreadyVoided, "This invoice has already been voided.");
        }

        if (invoice.Items.Any(i => i.PaymentStatus == Contracts.PaymentStatus.Paid))
        {
            return Result<InvoiceResponse>.Failure(BillingErrorCodes.HasPayments, "An invoice with a recorded payment cannot be voided. Record a refund or contact accounts first.");
        }

        invoice.Void(request.Reason, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<InvoiceResponse>.Success(invoice.ToResponse());
    }
}
