using HMS.Modules.ActivityLog.Application;
using HMS.Modules.ActivityLog.Contracts;
using HMS.Modules.Billing.Application.Abstractions;
using HMS.Modules.Billing.Application.Mapping;
using HMS.Modules.Billing.Contracts;
using HMS.Modules.Billing.Domain;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Billing.Application;

/// <summary>
/// OP bill revert workflow (ADR-106): billing staff raise a request with a reason, a Super Admin
/// approves or rejects it, and approval reverts the invoice. Public for the same CS0051 reason
/// as <see cref="IInvoiceService"/>. Who may decide is checked by the controller (the caller's
/// login type), not here — this service only knows the actor's id and name.
/// </summary>
public interface IInvoiceRevertService
{
    Task<Result<InvoiceRevertStatusResponse>> GetForInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken);

    Task<Result<InvoiceRevertRequestResponse>> CreateAsync(Guid invoiceId, CreateInvoiceRevertRequest request, Guid? actorId, string? actorName, CancellationToken cancellationToken);

    Task<PagedResult<InvoiceRevertRequestResponse>> GetPagedAsync(InvoiceRevertRequestListQuery query, CancellationToken cancellationToken);

    Task<int> GetPendingCountAsync(CancellationToken cancellationToken);

    Task<Result<InvoiceRevertRequestResponse>> ApproveAsync(Guid id, ApproveInvoiceRevertRequest request, Guid? actorId, string? actorName, CancellationToken cancellationToken);

    Task<Result<InvoiceRevertRequestResponse>> RejectAsync(Guid id, RejectInvoiceRevertRequest request, Guid? actorId, string? actorName, CancellationToken cancellationToken);
}

internal class InvoiceRevertService : IInvoiceRevertService
{
    private const int MaxPageSize = 100;

    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IInvoiceRevertRequestRepository _repository;
    private readonly IPatientVisitService _patientVisitService;
    private readonly IActivityLogService _activityLog;

    public InvoiceRevertService(
        IInvoiceRepository invoiceRepository,
        IInvoiceRevertRequestRepository repository,
        IPatientVisitService patientVisitService,
        IActivityLogService activityLog)
    {
        _invoiceRepository = invoiceRepository;
        _repository = repository;
        _patientVisitService = patientVisitService;
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

    /// <summary>
    /// Why <paramref name="invoice"/> can't be reverted, or null if it can. Only OP bills are
    /// covered: Pharmacy lines would also need their stock put back and InpatientCharge lines
    /// belong to an IPD final bill, so either one rules the invoice out, as does a visit of any
    /// type other than OP. A bill with no registered visit (VisitId falls back to PatientId — see
    /// CreateInvoiceRequest.VisitId) or whose visit can't be found is an OPD Billing Entry bill
    /// and counts as OP.
    /// </summary>
    private async Task<string?> GetIneligibleReasonAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        if (invoice.IsVoided)
        {
            return invoice.IsReverted ? "This bill has already been reverted." : "This bill has been voided.";
        }

        if (invoice.Items.Any(i => i.BillingType is BillingType.Pharmacy or BillingType.InpatientCharge))
        {
            return "Only OP bills can be reverted — this bill has pharmacy or inpatient charges.";
        }

        if (invoice.VisitId != invoice.PatientId)
        {
            var visit = await _patientVisitService.GetByIdAsync(invoice.PatientId, invoice.VisitId, cancellationToken);
            if (visit is { IsSuccess: true, Value: { } found } && found.VisitType != VisitType.OP)
            {
                return $"Only OP bills can be reverted — this bill belongs to a {found.VisitType} visit.";
            }
        }

        return null;
    }

    public async Task<Result<InvoiceRevertStatusResponse>> GetForInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken);
        if (invoice is null)
        {
            return Result<InvoiceRevertStatusResponse>.Failure(BillingErrorCodes.NotFound, $"Invoice '{invoiceId}' was not found.");
        }

        var requests = await _repository.GetByInvoiceIdAsync(invoiceId, cancellationToken);
        var ineligibleReason = requests.Any(r => r.Status == InvoiceRevertRequestStatus.Pending)
            ? "A revert request for this bill is already waiting for Super Admin approval."
            : await GetIneligibleReasonAsync(invoice, cancellationToken);

        return Result<InvoiceRevertStatusResponse>.Success(new InvoiceRevertStatusResponse
        {
            CanRequest = ineligibleReason is null,
            IneligibleReason = ineligibleReason,
            Requests = requests.Select(ToRevertResponse).ToList(),
        });
    }

    public async Task<Result<InvoiceRevertRequestResponse>> CreateAsync(Guid invoiceId, CreateInvoiceRevertRequest request, Guid? actorId, string? actorName, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken);
        if (invoice is null)
        {
            return Result<InvoiceRevertRequestResponse>.Failure(BillingErrorCodes.NotFound, $"Invoice '{invoiceId}' was not found.");
        }

        var existing = await _repository.GetByInvoiceIdAsync(invoiceId, cancellationToken);
        if (existing.Any(r => r.Status == InvoiceRevertRequestStatus.Pending))
        {
            return Result<InvoiceRevertRequestResponse>.Failure(BillingErrorCodes.RevertAlreadyPending, "A revert request for this bill is already waiting for Super Admin approval.");
        }

        var ineligibleReason = await GetIneligibleReasonAsync(invoice, cancellationToken);
        if (ineligibleReason is not null)
        {
            return Result<InvoiceRevertRequestResponse>.Failure(BillingErrorCodes.RevertNotEligible, ineligibleReason);
        }

        var revertRequest = InvoiceRevertRequest.Create(invoice, request.Reason, actorName, actorId);
        await _repository.AddAsync(revertRequest, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var response = ToRevertResponse(revertRequest);
        await LogAsync(ActivityLogActions.Create, "InvoiceRevertRequest", revertRequest.Id, $"Requested revert of invoice {invoice.InvoiceNumber}: {revertRequest.Reason}", null, response, actorId, cancellationToken);

        return Result<InvoiceRevertRequestResponse>.Success(response);
    }

    public async Task<PagedResult<InvoiceRevertRequestResponse>> GetPagedAsync(InvoiceRevertRequestListQuery query, CancellationToken cancellationToken)
    {
        var bounded = query with { Page = Math.Max(query.Page, 1), PageSize = Math.Clamp(query.PageSize, 1, MaxPageSize) };
        var (items, totalCount) = await _repository.GetPagedAsync(bounded, cancellationToken);
        return new PagedResult<InvoiceRevertRequestResponse>(items.Select(ToRevertResponse).ToList(), bounded.Page, bounded.PageSize, totalCount);
    }

    public Task<int> GetPendingCountAsync(CancellationToken cancellationToken) => _repository.CountPendingAsync(cancellationToken);

    public async Task<Result<InvoiceRevertRequestResponse>> ApproveAsync(Guid id, ApproveInvoiceRevertRequest request, Guid? actorId, string? actorName, CancellationToken cancellationToken)
    {
        var revertRequest = await _repository.GetByIdAsync(id, cancellationToken);
        if (revertRequest is null)
        {
            return Result<InvoiceRevertRequestResponse>.Failure(BillingErrorCodes.RevertRequestNotFound, $"Revert request '{id}' was not found.");
        }

        if (revertRequest.Status != InvoiceRevertRequestStatus.Pending)
        {
            return Result<InvoiceRevertRequestResponse>.Failure(BillingErrorCodes.RevertRequestNotPending, $"This revert request has already been {revertRequest.Status.ToString().ToLowerInvariant()}.");
        }

        var invoice = await _invoiceRepository.GetByIdAsync(revertRequest.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return Result<InvoiceRevertRequestResponse>.Failure(BillingErrorCodes.NotFound, $"Invoice '{revertRequest.InvoiceId}' was not found.");
        }

        if (invoice.IsVoided)
        {
            return Result<InvoiceRevertRequestResponse>.Failure(BillingErrorCodes.AlreadyVoided, "This bill has already been voided — reject the request instead.");
        }

        var invoiceBefore = invoice.ToResponse();

        // One SaveChanges for both: the request and its invoice share BillingDbContext.
        invoice.Revert(revertRequest.Reason, actorId);
        revertRequest.Approve(actorId, actorName, request.Notes);
        await _repository.SaveChangesAsync(cancellationToken);

        var response = ToRevertResponse(revertRequest);
        await LogAsync(ActivityLogActions.Update, "InvoiceRevertRequest", revertRequest.Id, $"Approved revert of invoice {invoice.InvoiceNumber}", null, response, actorId, cancellationToken);
        await LogAsync(ActivityLogActions.Void, "Invoice", invoice.Id, $"Reverted invoice {invoice.InvoiceNumber}: {revertRequest.Reason}", invoiceBefore, invoice.ToResponse(), actorId, cancellationToken);

        return Result<InvoiceRevertRequestResponse>.Success(response);
    }

    public async Task<Result<InvoiceRevertRequestResponse>> RejectAsync(Guid id, RejectInvoiceRevertRequest request, Guid? actorId, string? actorName, CancellationToken cancellationToken)
    {
        var revertRequest = await _repository.GetByIdAsync(id, cancellationToken);
        if (revertRequest is null)
        {
            return Result<InvoiceRevertRequestResponse>.Failure(BillingErrorCodes.RevertRequestNotFound, $"Revert request '{id}' was not found.");
        }

        if (revertRequest.Status != InvoiceRevertRequestStatus.Pending)
        {
            return Result<InvoiceRevertRequestResponse>.Failure(BillingErrorCodes.RevertRequestNotPending, $"This revert request has already been {revertRequest.Status.ToString().ToLowerInvariant()}.");
        }

        revertRequest.Reject(actorId, actorName, request.Notes);
        await _repository.SaveChangesAsync(cancellationToken);

        var response = ToRevertResponse(revertRequest);
        await LogAsync(ActivityLogActions.Update, "InvoiceRevertRequest", revertRequest.Id, $"Rejected revert of invoice {revertRequest.InvoiceNumber}: {revertRequest.DecisionNotes}", null, response, actorId, cancellationToken);

        return Result<InvoiceRevertRequestResponse>.Success(response);
    }

    private static InvoiceRevertRequestResponse ToRevertResponse(InvoiceRevertRequest r) => new()
    {
        Id = r.Id,
        InvoiceId = r.InvoiceId,
        InvoiceNumber = r.InvoiceNumber,
        PatientId = r.PatientId,
        PatientName = r.PatientName,
        PatientUhid = r.PatientUhid,
        NetAmount = r.NetAmount,
        PaidAmount = r.PaidAmount,
        Reason = r.Reason,
        Status = r.Status,
        RequestedBy = r.CreatedBy,
        RequestedByName = r.RequestedByName,
        RequestedAt = r.CreatedAt,
        DecidedBy = r.DecidedBy,
        DecidedByName = r.DecidedByName,
        DecidedAt = r.DecidedAt,
        DecisionNotes = r.DecisionNotes,
    };
}
