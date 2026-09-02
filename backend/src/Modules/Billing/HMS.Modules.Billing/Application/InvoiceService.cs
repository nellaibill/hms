using HMS.Modules.Billing.Application.Abstractions;
using HMS.Modules.Billing.Application.Mapping;
using HMS.Modules.Billing.Contracts;
using HMS.Modules.Billing.Domain;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Contracts;
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

    /// <summary>The latest <paramref name="count"/> invoices (any patient), each composed with
    /// its patient/visit context — backs the Patient Billing page's "Recent Patient Bills"
    /// table. See RecentPatientBillResponse for what's included and how missing patient/visit
    /// data is handled.</summary>
    Task<Result<IReadOnlyList<RecentPatientBillResponse>>> GetRecentAsync(int count, CancellationToken cancellationToken);

    Task<Result<InvoiceResponse>> RecordPaymentAsync(Guid invoiceId, Guid itemId, RecordPaymentRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<InvoiceResponse>> VoidAsync(Guid id, VoidInvoiceRequest request, Guid? actorId, CancellationToken cancellationToken);
}

internal class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _repository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IInvoiceNumberGenerator _numberGenerator;
    private readonly IPatientService _patientService;
    private readonly IPatientVisitService _patientVisitService;

    public InvoiceService(
        IInvoiceRepository repository,
        IPaymentRepository paymentRepository,
        IInvoiceNumberGenerator numberGenerator,
        IPatientService patientService,
        IPatientVisitService patientVisitService)
    {
        _repository = repository;
        _paymentRepository = paymentRepository;
        _numberGenerator = numberGenerator;
        _patientService = patientService;
        _patientVisitService = patientVisitService;
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

            if (totalTendered < invoice.NetAmount)
            {
                return Result<InvoiceResponse>.Failure(
                    BillingErrorCodes.PaymentAmountMismatch,
                    $"The amount received ({totalTendered:C}) is less than the invoice's net amount ({invoice.NetAmount:C}).");
            }

            // Cash is the only method that can realistically hand back change at the counter —
            // a Card/Upi/BankTransfer row can't be partially refunded on the spot, so those
            // rows must land within their actual share of the bill. E.g. a ₹700 bill paid as
            // ₹570 Upi + ₹200 Cash is fine (₹70 change comes back in cash); ₹700 Upi + ₹200 Cash
            // "just in case" isn't, since the Upi row alone already covers the whole bill with
            // nowhere for that excess to realistically come from. A single row may still
            // overtender regardless of method (this check only applies once split).
            if (payments.Count > 1)
            {
                var nonCashTendered = payments.Where(p => p.Method != PaymentMethod.Cash).Sum(p => p.Amount);
                if (nonCashTendered > invoice.NetAmount)
                {
                    return Result<InvoiceResponse>.Failure(
                        BillingErrorCodes.PaymentAmountMismatch,
                        $"Card, Upi, and BankTransfer amounts can't add up to more than the invoice's net amount ({invoice.NetAmount:C}) — any extra should be paid in Cash so change can be given back.");
                }
            }

            // Waterfall: apply each row's tendered amount against line items in order. Payment
            // still records per line item (see Domain/Payment.cs), so a row here may end up
            // split across more than one item, and an item may end up covered by more than one
            // row. Non-Cash rows are consumed before Cash rows regardless of the order they
            // were entered in, so any leftover (change) always lands on Cash — the validation
            // above guarantees the non-Cash rows alone never exceed NetAmount, so they're
            // always fully consumed by the time Cash rows are reached; only Cash can end up
            // with genuine, never-persisted leftover.
            var remaining = payments
                .OrderBy(p => p.Method == PaymentMethod.Cash)
                .Select(p => (p.Method, p.ReferenceNumber, Amount: p.Amount))
                .ToList();
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

    public async Task<Result<IReadOnlyList<RecentPatientBillResponse>>> GetRecentAsync(int count, CancellationToken cancellationToken)
    {
        var boundedCount = Math.Clamp(count, 1, 50);
        var (invoices, _) = await _repository.GetPagedAsync(
            new InvoiceListQuery { Page = 1, PageSize = boundedCount, Sort = "-createdAt" },
            cancellationToken);

        // Each invoice's patient/visit context is fetched via the other module's public
        // service — the same in-process composition CreateAsync already uses for patient
        // validation — deduplicated by patient (several recent bills can share a patient).
        // Sequential, not parallel: IPatientService/IPatientVisitService share one scoped
        // PatientsDbContext per request, and EF Core's DbContext throws
        // InvalidOperationException ("a second operation was started...") if two queries run
        // concurrently against it — confirmed live when Task.WhenAll on both lookups was
        // tried first.
        var patientCache = new Dictionary<Guid, PatientResponse?>();
        var rows = new List<RecentPatientBillResponse>(invoices.Count);
        foreach (var invoice in invoices)
        {
            if (!patientCache.TryGetValue(invoice.PatientId, out var patient))
            {
                var patientResult = await _patientService.GetByIdAsync(invoice.PatientId, cancellationToken);
                patient = patientResult.IsSuccess ? patientResult.Value : null;
                patientCache[invoice.PatientId] = patient;
            }

            var visitResult = await _patientVisitService.GetByIdAsync(invoice.PatientId, invoice.VisitId, cancellationToken);
            var visit = visitResult.IsSuccess ? visitResult.Value : null;

            rows.Add(new RecentPatientBillResponse
            {
                InvoiceId = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                PatientId = invoice.PatientId,
                PatientName = invoice.PatientName,
                PatientUhid = invoice.PatientUhid,
                Age = patient?.Age,
                Gender = patient?.Gender,
                ContactNumber = patient?.PrimaryPhone,
                RegistrationType = visit?.VisitType,
                Consultants = visit?.Consultations
                    .Select(c => new RecentBillConsultationResponse { DepartmentId = c.DepartmentId, ConsultantId = c.ConsultantId })
                    .ToList() ?? [],
                AppointmentDateTime = visit?.CreatedAt ?? invoice.CreatedAt,
                NetAmount = invoice.NetAmount,
                PaymentStatus = invoice.PaymentStatus,
                IsVoided = invoice.IsVoided,
            });
        }

        return Result<IReadOnlyList<RecentPatientBillResponse>>.Success(rows);
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

        if (invoice.IsVoided)
        {
            return Result<InvoiceResponse>.Failure(BillingErrorCodes.InvoiceVoided, $"Invoice '{invoiceId}' has been voided and can no longer receive payments.");
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
