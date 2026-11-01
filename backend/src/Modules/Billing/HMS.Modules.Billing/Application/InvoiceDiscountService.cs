using HMS.Modules.ActivityLog.Application;
using HMS.Modules.ActivityLog.Contracts;
using HMS.Modules.Billing.Application.Abstractions;
using HMS.Modules.Billing.Application.Mapping;
using HMS.Modules.Billing.Contracts;
using HMS.Modules.Billing.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Billing.Application;

/// <summary>
/// Bill discount workflow (ADR-109): billing staff ask for a discount on some or all lines of an
/// unpaid bill with a reason, a Super Admin approves or rejects it, and approval puts the
/// discount on the bill. A Super Admin's own request is approved as it is raised. Public for the
/// same CS0051 reason as <see cref="IInvoiceService"/>. Who may decide is checked by the
/// controller (the caller's login type), not here. A discount entered while the bill is being
/// made goes through InvoiceService.CreateAsync instead, using the same
/// <see cref="InvoiceDiscountRules"/>.
/// </summary>
public interface IInvoiceDiscountService
{
    Task<Result<InvoiceDiscountStatusResponse>> GetForInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken);

    Task<Result<InvoiceDiscountRequestResponse>> CreateAsync(Guid invoiceId, CreateInvoiceDiscountRequest request, BillingActor actor, CancellationToken cancellationToken);

    Task<PagedResult<InvoiceDiscountRequestResponse>> GetPagedAsync(InvoiceDiscountRequestListQuery query, CancellationToken cancellationToken);

    Task<int> GetPendingCountAsync(CancellationToken cancellationToken);

    Task<Result<InvoiceDiscountRequestResponse>> ApproveAsync(Guid id, ApproveInvoiceDiscountRequest request, Guid? actorId, string? actorName, CancellationToken cancellationToken);

    Task<Result<InvoiceDiscountRequestResponse>> RejectAsync(Guid id, RejectInvoiceDiscountRequest request, Guid? actorId, string? actorName, CancellationToken cancellationToken);
}

/// <summary>Which bills and lines can take a counter discount (ADR-109) — shared by
/// InvoiceDiscountService and InvoiceService.CreateAsync.</summary>
internal static class InvoiceDiscountRules
{
    /// <summary>The OPD billing cards' line types. Pharmacy sales have their own counter discount
    /// and InpatientCharge lines belong to an IPD final bill, so neither is covered.</summary>
    public static bool IsDiscountable(BillingType billingType) =>
        billingType is BillingType.Consultation or BillingType.Radiology or BillingType.Laboratory
            or BillingType.Procedure or BillingType.Injection or BillingType.File;

    /// <summary>A line a new request may cover: an OPD charge that is unpaid, has a price, and
    /// carries no discount yet — a discounted line isn't discounted a second time.</summary>
    public static bool CanTakeDiscount(InvoiceLineItem line) =>
        IsDiscountable(line.BillingType)
        && line.PaymentStatus == PaymentStatus.Pending
        && line.Discount == 0m
        && line.Quantity * line.UnitPrice > 0m;

    /// <summary>Why no new discount request may be raised on <paramref name="invoice"/>, or null.</summary>
    public static string? GetIneligibleReason(Invoice invoice)
    {
        if (invoice.IsVoided)
        {
            return "This bill has been voided.";
        }

        if (invoice.DiscountStatus == InvoiceDiscountRequestStatus.Pending)
        {
            return "A discount request for this bill is already waiting for Super Admin approval.";
        }

        return invoice.Items.Any(CanTakeDiscount)
            ? null
            : "This bill has no unpaid OP charges left to discount.";
    }

    /// <summary>Matches each requested line to the bill and checks it may take that discount.</summary>
    public static Result<IReadOnlyCollection<(InvoiceLineItem Line, decimal Discount)>> ResolveLines(Invoice invoice, IReadOnlyList<InvoiceDiscountLineRequest> lines)
    {
        var resolved = new List<(InvoiceLineItem Line, decimal Discount)>();
        foreach (var requested in lines)
        {
            var line = invoice.Items.FirstOrDefault(i => i.Id == requested.LineItemId);
            if (line is null)
            {
                return Result<IReadOnlyCollection<(InvoiceLineItem, decimal)>>.Failure(BillingErrorCodes.LineItemNotFound, $"Line item '{requested.LineItemId}' was not found on invoice {invoice.InvoiceNumber}.");
            }

            if (!CanTakeDiscount(line))
            {
                return Result<IReadOnlyCollection<(InvoiceLineItem, decimal)>>.Failure(BillingErrorCodes.DiscountNotEligible, "Only unpaid OP charges without a discount can be discounted — reload the bill and try again.");
            }

            if (requested.Discount > line.Quantity * line.UnitPrice)
            {
                return Result<IReadOnlyCollection<(InvoiceLineItem, decimal)>>.Failure(BillingErrorCodes.DiscountNotEligible, "A line's discount can't be more than its charge.");
            }

            resolved.Add((line, requested.Discount));
        }

        return Result<IReadOnlyCollection<(InvoiceLineItem, decimal)>>.Success(resolved);
    }
}

internal class InvoiceDiscountService : IInvoiceDiscountService
{
    private const int MaxPageSize = 100;

    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IInvoiceDiscountRequestRepository _repository;
    private readonly IActivityLogService _activityLog;

    public InvoiceDiscountService(
        IInvoiceRepository invoiceRepository,
        IInvoiceDiscountRequestRepository repository,
        IActivityLogService activityLog)
    {
        _invoiceRepository = invoiceRepository;
        _repository = repository;
        _activityLog = activityLog;
    }

    // Best-effort audit write (never throws - see IActivityLogService); runs only after the
    // change itself has been committed.
    private Task LogAsync(string action, string entityType, Guid entityId, string description, object? oldValues, object? newValues, Guid? actorId, CancellationToken cancellationToken)
        => _activityLog.LogAsync(new ActivityLogRequest
        {
            Action = action,
            Module = ActivityLogModules.Billing,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            Description = description,
            OldValues = oldValues,
            NewValues = newValues,
            UserId = actorId,
        }, cancellationToken);

    public async Task<Result<InvoiceDiscountStatusResponse>> GetForInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken);
        if (invoice is null)
        {
            return Result<InvoiceDiscountStatusResponse>.Failure(BillingErrorCodes.NotFound, $"Invoice '{invoiceId}' was not found.");
        }

        var requests = await _repository.GetByInvoiceIdAsync(invoiceId, cancellationToken);
        var ineligibleReason = InvoiceDiscountRules.GetIneligibleReason(invoice);

        return Result<InvoiceDiscountStatusResponse>.Success(new InvoiceDiscountStatusResponse
        {
            CanRequest = ineligibleReason is null,
            IneligibleReason = ineligibleReason,
            Requests = requests.Select(r => r.ToResponse()).ToList(),
        });
    }

    public async Task<Result<InvoiceDiscountRequestResponse>> CreateAsync(Guid invoiceId, CreateInvoiceDiscountRequest request, BillingActor actor, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken);
        if (invoice is null)
        {
            return Result<InvoiceDiscountRequestResponse>.Failure(BillingErrorCodes.NotFound, $"Invoice '{invoiceId}' was not found.");
        }

        var ineligibleReason = InvoiceDiscountRules.GetIneligibleReason(invoice);
        if (ineligibleReason is not null)
        {
            return Result<InvoiceDiscountRequestResponse>.Failure(
                invoice.DiscountStatus == InvoiceDiscountRequestStatus.Pending ? BillingErrorCodes.DiscountAlreadyPending : BillingErrorCodes.DiscountNotEligible,
                ineligibleReason);
        }

        var lines = InvoiceDiscountRules.ResolveLines(invoice, request.Lines);
        if (!lines.IsSuccess)
        {
            return Result<InvoiceDiscountRequestResponse>.Failure(lines.ErrorCode!, lines.Error!);
        }

        var invoiceBefore = invoice.ToResponse();
        var discountRequest = InvoiceDiscountRequest.Create(invoice, lines.Value!, request.DiscountType, request.DiscountValue, request.Reason, actor.UserName, actor.UserId);
        if (actor.IsSuperAdmin)
        {
            invoice.ApplyDiscount(discountRequest.LineDiscounts(), actor.UserName, actor.UserId);
            discountRequest.Approve(actor.UserId, actor.UserName, null);
        }
        else
        {
            invoice.MarkDiscountPending(actor.UserId);
        }

        await _repository.AddAsync(discountRequest, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var response = discountRequest.ToResponse();
        await LogAsync(ActivityLogActions.Create, "InvoiceDiscountRequest", discountRequest.Id, $"Requested a discount of {discountRequest.TotalDiscount} on invoice {invoice.InvoiceNumber}: {discountRequest.Reason}", null, response, actor.UserId, cancellationToken);
        if (actor.IsSuperAdmin)
        {
            await LogAsync(ActivityLogActions.Update, "Invoice", invoice.Id, $"Applied a discount of {discountRequest.TotalDiscount} to invoice {invoice.InvoiceNumber}", invoiceBefore, invoice.ToResponse(), actor.UserId, cancellationToken);
        }

        return Result<InvoiceDiscountRequestResponse>.Success(response);
    }

    public async Task<PagedResult<InvoiceDiscountRequestResponse>> GetPagedAsync(InvoiceDiscountRequestListQuery query, CancellationToken cancellationToken)
    {
        var bounded = query with { Page = Math.Max(query.Page, 1), PageSize = Math.Clamp(query.PageSize, 1, MaxPageSize) };
        var (items, totalCount) = await _repository.GetPagedAsync(bounded, cancellationToken);
        return new PagedResult<InvoiceDiscountRequestResponse>(items.Select(r => r.ToResponse()).ToList(), bounded.Page, bounded.PageSize, totalCount);
    }

    public Task<int> GetPendingCountAsync(CancellationToken cancellationToken) => _repository.CountPendingAsync(cancellationToken);

    public async Task<Result<InvoiceDiscountRequestResponse>> ApproveAsync(Guid id, ApproveInvoiceDiscountRequest request, Guid? actorId, string? actorName, CancellationToken cancellationToken)
    {
        var discountRequest = await _repository.GetByIdAsync(id, cancellationToken);
        if (discountRequest is null)
        {
            return Result<InvoiceDiscountRequestResponse>.Failure(BillingErrorCodes.DiscountRequestNotFound, $"Discount request '{id}' was not found.");
        }

        if (discountRequest.Status != InvoiceDiscountRequestStatus.Pending)
        {
            return Result<InvoiceDiscountRequestResponse>.Failure(BillingErrorCodes.DiscountRequestNotPending, $"This discount request has already been {discountRequest.Status.ToString().ToLowerInvariant()}.");
        }

        var invoice = await _invoiceRepository.GetByIdAsync(discountRequest.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return Result<InvoiceDiscountRequestResponse>.Failure(BillingErrorCodes.NotFound, $"Invoice '{discountRequest.InvoiceId}' was not found.");
        }

        if (invoice.IsVoided)
        {
            return Result<InvoiceDiscountRequestResponse>.Failure(BillingErrorCodes.AlreadyVoided, "This bill has been voided — reject the request instead.");
        }

        // Payments are blocked while the request waits, so this only trips on data changed some
        // other way — but a paid line must never be re-priced.
        var lineIds = discountRequest.Lines.Select(l => l.LineItemId).ToHashSet();
        if (invoice.Items.Where(i => lineIds.Contains(i.Id)).Count() != lineIds.Count
            || invoice.Items.Any(i => lineIds.Contains(i.Id) && !InvoiceDiscountRules.CanTakeDiscount(i)))
        {
            return Result<InvoiceDiscountRequestResponse>.Failure(BillingErrorCodes.DiscountNotEligible, "The bill has changed since this discount was requested (a line was paid or already discounted) — reject the request instead.");
        }

        var invoiceBefore = invoice.ToResponse();

        // One SaveChanges for both: the request and its invoice share BillingDbContext.
        invoice.ApplyDiscount(discountRequest.LineDiscounts(), actorName, actorId);
        discountRequest.Approve(actorId, actorName, request.Notes);
        await _repository.SaveChangesAsync(cancellationToken);

        var response = discountRequest.ToResponse();
        await LogAsync(ActivityLogActions.Update, "InvoiceDiscountRequest", discountRequest.Id, $"Approved a discount of {discountRequest.TotalDiscount} on invoice {invoice.InvoiceNumber}", null, response, actorId, cancellationToken);
        await LogAsync(ActivityLogActions.Update, "Invoice", invoice.Id, $"Applied a discount of {discountRequest.TotalDiscount} to invoice {invoice.InvoiceNumber}", invoiceBefore, invoice.ToResponse(), actorId, cancellationToken);

        return Result<InvoiceDiscountRequestResponse>.Success(response);
    }

    public async Task<Result<InvoiceDiscountRequestResponse>> RejectAsync(Guid id, RejectInvoiceDiscountRequest request, Guid? actorId, string? actorName, CancellationToken cancellationToken)
    {
        var discountRequest = await _repository.GetByIdAsync(id, cancellationToken);
        if (discountRequest is null)
        {
            return Result<InvoiceDiscountRequestResponse>.Failure(BillingErrorCodes.DiscountRequestNotFound, $"Discount request '{id}' was not found.");
        }

        if (discountRequest.Status != InvoiceDiscountRequestStatus.Pending)
        {
            return Result<InvoiceDiscountRequestResponse>.Failure(BillingErrorCodes.DiscountRequestNotPending, $"This discount request has already been {discountRequest.Status.ToString().ToLowerInvariant()}.");
        }

        var invoice = await _invoiceRepository.GetByIdAsync(discountRequest.InvoiceId, cancellationToken);
        if (invoice is { DiscountStatus: InvoiceDiscountRequestStatus.Pending })
        {
            invoice.MarkDiscountRejected(actorId);
        }

        discountRequest.Reject(actorId, actorName, request.Notes);
        await _repository.SaveChangesAsync(cancellationToken);

        var response = discountRequest.ToResponse();
        await LogAsync(ActivityLogActions.Update, "InvoiceDiscountRequest", discountRequest.Id, $"Rejected a discount on invoice {discountRequest.InvoiceNumber}: {discountRequest.DecisionNotes}", null, response, actorId, cancellationToken);

        return Result<InvoiceDiscountRequestResponse>.Success(response);
    }
}
