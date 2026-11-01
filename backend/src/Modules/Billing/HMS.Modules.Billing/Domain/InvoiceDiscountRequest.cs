using HMS.Modules.Billing.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Billing.Domain;

/// <summary>
/// A request for a discount on some or all lines of an unpaid bill, raised by billing staff
/// with a mandatory reason and decided by a Super Admin (ADR-109). Approving it puts the
/// discounts on the bill — see <see cref="Invoice.ApplyDiscount"/>. A Super Admin's own
/// discount is recorded the same way, already approved, so every discount has one audit trail.
/// Invoice number, patient and each line's charge are snapshotted at creation, like
/// <see cref="InvoiceRevertRequest"/>, so the approval list stays readable on its own.
/// Approve/Reject/Cancel are only valid from Pending; InvoiceDiscountService checks Status first.
/// </summary>
internal class InvoiceDiscountRequest : Entity
{
    public Guid InvoiceId { get; private set; }
    public string InvoiceNumber { get; private set; } = null!;
    public Guid PatientId { get; private set; }
    public string PatientName { get; private set; } = null!;
    public string PatientUhid { get; private set; } = null!;
    public decimal GrossAmount { get; private set; }
    public InvoiceDiscountType DiscountType { get; private set; }
    public decimal DiscountValue { get; private set; }
    public decimal TotalDiscount { get; private set; }
    public string Reason { get; private set; } = null!;
    public InvoiceDiscountRequestStatus Status { get; private set; }

    /// <summary>Usernames are snapshotted from the caller's token — Billing has no access to
    /// Identity's user directory (see HMS.ArchitectureTests' cross-module rules).</summary>
    public string? RequestedByName { get; private set; }

    public Guid? DecidedBy { get; private set; }
    public string? DecidedByName { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public string? DecisionNotes { get; private set; }

    private readonly List<InvoiceDiscountRequestLine> _lines = [];
    public IReadOnlyCollection<InvoiceDiscountRequestLine> Lines => _lines.AsReadOnly();

    // Required by EF Core materialization.
    private InvoiceDiscountRequest()
    {
    }

    private InvoiceDiscountRequest(
        Guid id,
        Invoice invoice,
        InvoiceDiscountType discountType,
        decimal discountValue,
        string reason,
        string? requestedByName,
        Guid? createdBy)
        : base(id, createdBy)
    {
        InvoiceId = invoice.Id;
        InvoiceNumber = invoice.InvoiceNumber;
        PatientId = invoice.PatientId;
        PatientName = invoice.PatientName;
        PatientUhid = invoice.PatientUhid;
        GrossAmount = invoice.GrossAmount;
        DiscountType = discountType;
        DiscountValue = discountValue;
        Reason = reason;
        Status = InvoiceDiscountRequestStatus.Pending;
        RequestedByName = requestedByName;
    }

    /// <summary>Caller has already checked every line is on <paramref name="invoice"/>, may be
    /// discounted, and that each discount is above zero and within the line's charge.</summary>
    public static InvoiceDiscountRequest Create(
        Invoice invoice,
        IReadOnlyCollection<(InvoiceLineItem Line, decimal Discount)> lines,
        InvoiceDiscountType discountType,
        decimal discountValue,
        string reason,
        string? requestedByName,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(reason, nameof(reason));
        if (lines.Count == 0)
        {
            throw new ArgumentException("A discount request needs at least one line.", nameof(lines));
        }

        var request = new InvoiceDiscountRequest(
            Guid.CreateVersion7(),
            invoice,
            discountType,
            discountValue,
            reason.Trim(),
            string.IsNullOrWhiteSpace(requestedByName) ? null : requestedByName.Trim(),
            createdBy);

        foreach (var (line, discount) in lines)
        {
            request._lines.Add(InvoiceDiscountRequestLine.Create(request.Id, line, discount, createdBy));
        }

        request.TotalDiscount = request._lines.Sum(l => l.Discount);
        return request;
    }

    /// <summary>The (line, discount) pairs to hand to <see cref="Invoice.ApplyDiscount"/>.</summary>
    public IReadOnlyCollection<(Guid LineItemId, decimal Discount)> LineDiscounts()
        => _lines.Select(l => (l.LineItemId, l.Discount)).ToList();

    public void Approve(Guid? actorId, string? actorName, string? notes) => Decide(InvoiceDiscountRequestStatus.Approved, actorId, actorName, notes);

    public void Reject(Guid? actorId, string? actorName, string? notes) => Decide(InvoiceDiscountRequestStatus.Rejected, actorId, actorName, notes);

    /// <summary>The bill was voided while this request was still waiting.</summary>
    public void Cancel(Guid? actorId, string? notes) => Decide(InvoiceDiscountRequestStatus.Cancelled, actorId, null, notes);

    private void Decide(InvoiceDiscountRequestStatus status, Guid? actorId, string? actorName, string? notes)
    {
        Status = status;
        DecidedBy = actorId;
        DecidedByName = string.IsNullOrWhiteSpace(actorName) ? null : actorName.Trim();
        DecidedAt = DateTime.UtcNow;
        DecisionNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        MarkUpdated(actorId);
    }
}

/// <summary>One bill line's share of an <see cref="InvoiceDiscountRequest"/>, with the line's
/// identity and charge copied from <see cref="InvoiceLineItem"/> when the request was raised.</summary>
internal class InvoiceDiscountRequestLine : Entity
{
    public Guid RequestId { get; private set; }
    public Guid LineItemId { get; private set; }
    public BillingType BillingType { get; private set; }
    public string? DepartmentId { get; private set; }
    public string? ConsultantId { get; private set; }
    public string? ServiceId { get; private set; }
    public Guid? PackageId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Discount { get; private set; }

    // Required by EF Core materialization.
    private InvoiceDiscountRequestLine()
    {
    }

    private InvoiceDiscountRequestLine(Guid id, Guid requestId, InvoiceLineItem line, decimal discount, Guid? createdBy)
        : base(id, createdBy)
    {
        RequestId = requestId;
        LineItemId = line.Id;
        BillingType = line.BillingType;
        DepartmentId = line.DepartmentId;
        // A paid line has cleared ConsultantId (ADR-048) — only unpaid lines get here, but the
        // billed consultant is the one to show either way.
        ConsultantId = line.BilledConsultantId;
        ServiceId = line.ServiceId;
        PackageId = line.PackageId;
        Quantity = line.Quantity;
        UnitPrice = line.UnitPrice;
        Discount = discount;
    }

    public static InvoiceDiscountRequestLine Create(Guid requestId, InvoiceLineItem line, decimal discount, Guid? createdBy)
        => new(Guid.CreateVersion7(), requestId, line, discount, createdBy);
}
