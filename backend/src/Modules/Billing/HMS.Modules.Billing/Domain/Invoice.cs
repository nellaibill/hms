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
}
