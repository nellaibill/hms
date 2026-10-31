using HMS.Modules.Billing.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Billing.Domain;

/// <summary>One line to create, passed into <see cref="Invoice.Create"/> — the Application
/// layer's shape for a not-yet-persisted <see cref="InvoiceLineItem"/>. Internal like the
/// rest of Domain/Application (docs/DecisionLog.md ADR-006): Contracts' own
/// CreateInvoiceLineItemRequest is the public-facing equivalent InvoiceService maps from.</summary>
internal sealed record InvoiceLineItemSpec(
    BillingType BillingType,
    string? DepartmentId,
    string? ConsultantId,
    string? ServiceId,
    Guid? PackageId,
    int Quantity,
    decimal UnitPrice,
    decimal Discount,
    bool DiscountApproved,
    string? DiscountApprovedBy);

/// <summary>
/// The bill for one visit — Reception &amp; Registration's Billing step and the standalone OPD
/// Billing Entry screen both create one of these per patient encounter. Patient name/UHID
/// are snapshotted at creation rather than a live join to Patients (see
/// frontend/web/src/features/billing/types.ts's identical rationale, and
/// docs/DecisionLog.md ADR-008 on why Billing stays out of Patient-edit's blast radius): an
/// invoice should keep showing who it was billed to even if the patient record is edited
/// later. Gross/discount/net totals are computed once at creation from the line items given
/// and stored, not recomputed on every read — a saved invoice is a settled transaction
/// record, not a live-recalculating view.
/// </summary>
internal class Invoice : Entity
{
    public string InvoiceNumber { get; private set; } = null!;
    public Guid PatientId { get; private set; }
    public Guid VisitId { get; private set; }
    public string PatientName { get; private set; } = null!;
    public string PatientUhid { get; private set; } = null!;

    public decimal GrossAmount { get; private set; }
    public decimal TotalDiscount { get; private set; }
    public decimal NetAmount { get; private set; }

    private readonly List<InvoiceLineItem> _items = [];
    public IReadOnlyCollection<InvoiceLineItem> Items => _items.AsReadOnly();

    public bool IsVoided { get; private set; }
    public DateTime? VoidedAt { get; private set; }
    public Guid? VoidedBy { get; private set; }
    public string? VoidReason { get; private set; }

    /// <summary>True when the void came from an approved revert request (ADR-106) rather than a
    /// plain <see cref="Void"/> — <see cref="IsVoided"/> is set either way.</summary>
    public bool IsReverted { get; private set; }

    /// <summary>The state of this bill's latest <see cref="InvoiceDiscountRequest"/> (ADR-109) —
    /// null when none was ever raised. While Pending the bill can't take a payment: the amount
    /// owed isn't settled until a Super Admin decides.</summary>
    public InvoiceDiscountRequestStatus? DiscountStatus { get; private set; }

    /// <summary>Derived, not stored — Paid only once every line item is Paid, matching
    /// frontend/web/src/features/billing/billingCalculations.ts's getOverallPaymentStatus.</summary>
    public PaymentStatus PaymentStatus =>
        _items.Count > 0 && _items.All(i => i.PaymentStatus == Contracts.PaymentStatus.Paid)
            ? Contracts.PaymentStatus.Paid
            : Contracts.PaymentStatus.Pending;

    // Required by EF Core materialization.
    private Invoice()
    {
    }

    private Invoice(
        Guid id,
        string invoiceNumber,
        Guid patientId,
        Guid visitId,
        string patientName,
        string patientUhid,
        Guid? createdBy)
        : base(id, createdBy)
    {
        InvoiceNumber = invoiceNumber;
        PatientId = patientId;
        VisitId = visitId;
        PatientName = patientName;
        PatientUhid = patientUhid;
    }

    /// <summary>Builds the invoice and every one of its line items in one call — matches the
    /// UI's actual use case (a bill is entered and saved as one counter transaction, not
    /// assembled incrementally against a persisted draft). Rejects an empty item list: an
    /// invoice with nothing billed isn't a record worth keeping, mirroring
    /// mockBillingStore.ts's saveBillingForPatient returning null for the same case —
    /// enforced by InvoiceService/CreateInvoiceRequestValidator before this is ever called,
    /// so Create itself can assume `items` is non-empty.</summary>
    public static Invoice Create(
        string invoiceNumber,
        Guid patientId,
        Guid visitId,
        string patientName,
        string patientUhid,
        IReadOnlyList<InvoiceLineItemSpec> items,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(invoiceNumber, nameof(invoiceNumber));
        Guard.AgainstNullOrWhiteSpace(patientName, nameof(patientName));
        Guard.AgainstNullOrWhiteSpace(patientUhid, nameof(patientUhid));

        var invoice = new Invoice(
            Guid.CreateVersion7(),
            invoiceNumber.Trim(),
            patientId,
            visitId,
            patientName.Trim(),
            patientUhid.Trim(),
            createdBy);

        foreach (var spec in items)
        {
            invoice._items.Add(InvoiceLineItem.Create(
                invoice.Id,
                spec.BillingType,
                spec.DepartmentId,
                spec.ConsultantId,
                spec.ServiceId,
                spec.PackageId,
                spec.Quantity,
                spec.UnitPrice,
                spec.Discount,
                spec.DiscountApproved,
                spec.DiscountApprovedBy,
                createdBy));
        }

        invoice.GrossAmount = invoice._items.Sum(i => i.Quantity * i.UnitPrice);
        invoice.TotalDiscount = invoice._items.Sum(i => i.Discount);
        invoice.NetAmount = Math.Max(invoice.GrossAmount - invoice.TotalDiscount, 0m);

        return invoice;
    }

    /// <summary>Caller (InvoiceService) is responsible for creating the corresponding
    /// Payment record and has already verified <paramref name="itemId"/> belongs to this
    /// invoice and isn't already Paid.</summary>
    public InvoiceLineItem MarkItemPaid(Guid itemId, Guid? updatedBy)
    {
        var item = _items.First(i => i.Id == itemId);
        item.MarkPaid(updatedBy);
        return item;
    }

    /// <summary>
    /// Cancels the invoice — a mis-billed invoice can otherwise never be corrected, only
    /// superseded by a second one, leaving the wrong one live forever. Deliberately not the
    /// base Entity's SoftDelete: a voided invoice must stay visible/queryable (it's still a
    /// real record for audit purposes), not disappear from every query like a deleted row.
    /// Blocked once any payment has been recorded — reversing real collected money needs a
    /// refund process, not a void, and no refund feature exists yet. Caller (InvoiceService)
    /// has already checked both preconditions (not already voided, no paid items) so it can
    /// return a proper Result.Failure instead of this throwing; the checks are repeated here
    /// too since they're genuine invariants of a voided Invoice, not just HTTP-layer concerns.
    /// </summary>
    public void Void(string reason, Guid? voidedBy)
    {
        Guard.AgainstNullOrWhiteSpace(reason, nameof(reason));
        if (IsVoided)
        {
            throw new InvalidOperationException("This invoice has already been voided.");
        }
        if (_items.Any(i => i.PaymentStatus == Contracts.PaymentStatus.Paid))
        {
            throw new InvalidOperationException("An invoice with a recorded payment cannot be voided.");
        }

        IsVoided = true;
        VoidedAt = DateTime.UtcNow;
        VoidedBy = voidedBy;
        VoidReason = reason.Trim();
        if (DiscountStatus == InvoiceDiscountRequestStatus.Pending)
        {
            DiscountStatus = InvoiceDiscountRequestStatus.Cancelled;
        }
        MarkUpdated(voidedBy);
    }

    /// <summary>A discount request on this bill is now waiting for a Super Admin (ADR-109).</summary>
    public void MarkDiscountPending(Guid? updatedBy)
    {
        DiscountStatus = InvoiceDiscountRequestStatus.Pending;
        MarkUpdated(updatedBy);
    }

    /// <summary>The Super Admin turned the bill's discount request down — the bill stays at the
    /// price it had and can be paid again.</summary>
    public void MarkDiscountRejected(Guid? updatedBy)
    {
        DiscountStatus = InvoiceDiscountRequestStatus.Rejected;
        MarkUpdated(updatedBy);
    }

    /// <summary>
    /// Applies an approved discount (ADR-109) to the given lines and recomputes the bill's
    /// discount and net totals. Callers (InvoiceService for a Super Admin's own bill,
    /// InvoiceDiscountService on approval) have already checked the bill isn't voided and every
    /// line is on it and unpaid.
    /// </summary>
    public void ApplyDiscount(IReadOnlyCollection<(Guid LineItemId, decimal Discount)> lines, string? approvedByName, Guid? approvedBy)
    {
        if (IsVoided)
        {
            throw new InvalidOperationException("A voided invoice can't be discounted.");
        }

        foreach (var (lineItemId, discount) in lines)
        {
            _items.First(i => i.Id == lineItemId).ApplyDiscount(discount, approvedByName, approvedBy);
        }

        TotalDiscount = _items.Sum(i => i.Discount);
        NetAmount = Math.Max(GrossAmount - TotalDiscount, 0m);
        DiscountStatus = InvoiceDiscountRequestStatus.Approved;
        MarkUpdated(approvedBy);
    }

    /// <summary>
    /// Reverts an OP bill once a Super Admin approves its <see cref="InvoiceRevertRequest"/>
    /// (ADR-106). Unlike <see cref="Void"/> this is allowed after payment: the invoice is voided
    /// (so every report and ledger total drops it) and flagged <see cref="IsReverted"/>, while
    /// its line items and Payment rows stay as they were — the record of what was collected
    /// before the counter refunded it. Caller (InvoiceRevertService) has already checked the
    /// invoice isn't voided.
    /// </summary>
    public void Revert(string reason, Guid? approvedBy)
    {
        Guard.AgainstNullOrWhiteSpace(reason, nameof(reason));
        if (IsVoided)
        {
            throw new InvalidOperationException("This invoice has already been voided.");
        }

        IsVoided = true;
        IsReverted = true;
        VoidedAt = DateTime.UtcNow;
        VoidedBy = approvedBy;
        VoidReason = reason.Trim();
        MarkUpdated(approvedBy);
    }
}
