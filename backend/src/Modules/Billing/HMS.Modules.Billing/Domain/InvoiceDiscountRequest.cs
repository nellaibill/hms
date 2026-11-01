using HMS.Modules.Billing.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Billing.Domain;

/// <summary>
/// A request for a discount on some or all lines of an unpaid bill, raised by billing staff
/// with a mandatory reason (ADR-109). Its lines are split into approval parts (ADR-111): a
/// consultation fee's discount goes to that consultant, every other line to the Super Admin, and
/// each part is decided on its own — approving a part puts its lines' discounts on the bill (see
/// <see cref="Invoice.ApplyDiscount"/>). A Super Admin may decide any part, a consultant's
/// included. The request stays Pending until every part is decided. A Super Admin's own discount
/// is recorded the same way, every part already approved, so every discount has one audit trail.
/// Invoice number, patient and each line's charge are snapshotted at creation, like
/// <see cref="InvoiceRevertRequest"/>, so the approval list stays readable on its own.
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

    /// <summary>Whoever made the decision that settled the request (the last part decided), and
    /// when — each part keeps its own decision in <see cref="Approvals"/>.</summary>
    public Guid? DecidedBy { get; private set; }
    public string? DecidedByName { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public string? DecisionNotes { get; private set; }

    private readonly List<InvoiceDiscountRequestLine> _lines = [];
    public IReadOnlyCollection<InvoiceDiscountRequestLine> Lines => _lines.AsReadOnly();

    private readonly List<InvoiceDiscountApproval> _approvals = [];
    public IReadOnlyCollection<InvoiceDiscountApproval> Approvals => _approvals.AsReadOnly();

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

    /// <summary>The consultant who decides a line's discount (ADR-111): the billed consultant of
    /// a Consultation line — it's their fee. Null for every other line (and a consultation line
    /// with no consultant), which the Super Admin decides.</summary>
    public static Guid? ApproverConsultantFor(InvoiceLineItem line) =>
        line.BillingType == BillingType.Consultation && Guid.TryParse(line.BilledConsultantId, out var consultantId)
            ? consultantId
            : null;

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

        // One part per consultant, in the order their lines appear; the Super Admin's part last.
        var groups = lines
            .GroupBy(l => ApproverConsultantFor(l.Line))
            .OrderBy(g => g.Key is null ? 1 : 0);
        foreach (var group in groups)
        {
            var approval = InvoiceDiscountApproval.Create(request.Id, group.Key, createdBy);
            request._approvals.Add(approval);
            foreach (var (line, discount) in group)
            {
                request._lines.Add(InvoiceDiscountRequestLine.Create(request.Id, approval.Id, line, discount, createdBy));
            }
        }

        request.TotalDiscount = request._lines.Sum(l => l.Discount);
        return request;
    }

    public InvoiceDiscountApproval? FindApproval(Guid approvalId) => _approvals.FirstOrDefault(a => a.Id == approvalId);

    /// <summary>Every (line, discount) pair, for <see cref="Invoice.ApplyDiscount"/>.</summary>
    public IReadOnlyCollection<(Guid LineItemId, decimal Discount)> LineDiscounts()
        => _lines.Select(l => (l.LineItemId, l.Discount)).ToList();

    /// <summary>One part's (line, discount) pairs, for <see cref="Invoice.ApplyDiscount"/>.</summary>
    public IReadOnlyCollection<(Guid LineItemId, decimal Discount)> LineDiscountsFor(Guid approvalId)
        => _lines.Where(l => l.ApprovalId == approvalId).Select(l => (l.LineItemId, l.Discount)).ToList();

    /// <summary>Sum of the discounts in approved parts.</summary>
    public decimal ApprovedDiscount()
    {
        var approvedIds = _approvals.Where(a => a.Status == InvoiceDiscountRequestStatus.Approved).Select(a => a.Id).ToHashSet();
        return _lines.Where(l => approvedIds.Contains(l.ApprovalId)).Sum(l => l.Discount);
    }

    /// <summary>Caller has checked the part exists and is Pending, and that the actor may decide it.</summary>
    public void ApprovePart(Guid approvalId, Guid? actorId, string? actorName, string? notes, bool onBehalf)
        => DecidePart(approvalId, InvoiceDiscountRequestStatus.Approved, actorId, actorName, notes, onBehalf);

    /// <summary>Caller has checked the part exists and is Pending, and that the actor may decide it.</summary>
    public void RejectPart(Guid approvalId, Guid? actorId, string? actorName, string? notes, bool onBehalf)
        => DecidePart(approvalId, InvoiceDiscountRequestStatus.Rejected, actorId, actorName, notes, onBehalf);

    /// <summary>A Super Admin's own discount: every part approved as it's raised, a consultant's
    /// part recorded as decided on their behalf.</summary>
    public void ApproveAll(Guid? actorId, string? actorName)
    {
        foreach (var approval in _approvals.Where(a => a.Status == InvoiceDiscountRequestStatus.Pending))
        {
            approval.Decide(InvoiceDiscountRequestStatus.Approved, actorId, actorName, null, onBehalf: approval.ApproverType == InvoiceDiscountApproverType.Consultant);
        }

        Settle(actorId, actorName, null);
    }

    /// <summary>The bill was voided while this request was still waiting — every part still
    /// waiting is cancelled with it.</summary>
    public void Cancel(Guid? actorId, string? notes)
    {
        foreach (var approval in _approvals.Where(a => a.Status == InvoiceDiscountRequestStatus.Pending))
        {
            approval.Decide(InvoiceDiscountRequestStatus.Cancelled, actorId, null, notes, onBehalf: false);
        }

        Status = InvoiceDiscountRequestStatus.Cancelled;
        Stamp(actorId, null, notes);
    }

    private void DecidePart(Guid approvalId, InvoiceDiscountRequestStatus status, Guid? actorId, string? actorName, string? notes, bool onBehalf)
    {
        var approval = _approvals.First(a => a.Id == approvalId);
        approval.Decide(status, actorId, actorName, notes, onBehalf);
        Settle(actorId, actorName, notes);
    }

    /// <summary>Works out the request's status from its parts: Pending while any part waits.</summary>
    private void Settle(Guid? actorId, string? actorName, string? notes)
    {
        if (_approvals.Any(a => a.Status == InvoiceDiscountRequestStatus.Pending))
        {
            MarkUpdated(actorId);
            return;
        }

        var approved = _approvals.Count(a => a.Status == InvoiceDiscountRequestStatus.Approved);
        Status = approved == _approvals.Count
            ? InvoiceDiscountRequestStatus.Approved
            : approved == 0
                ? InvoiceDiscountRequestStatus.Rejected
                : InvoiceDiscountRequestStatus.PartlyApproved;
        Stamp(actorId, actorName, notes);
    }

    private void Stamp(Guid? actorId, string? actorName, string? notes)
    {
        DecidedBy = actorId;
        DecidedByName = string.IsNullOrWhiteSpace(actorName) ? null : actorName.Trim();
        DecidedAt = DateTime.UtcNow;
        DecisionNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        MarkUpdated(actorId);
    }
}

/// <summary>One approver's part of an <see cref="InvoiceDiscountRequest"/> (ADR-111): the
/// consultant whose consultation fee is discounted, or the Super Admin for every other line.
/// Decided once — Approved, Rejected, or Cancelled with a voided bill.</summary>
internal class InvoiceDiscountApproval : Entity
{
    public Guid RequestId { get; private set; }
    public InvoiceDiscountApproverType ApproverType { get; private set; }

    /// <summary>Masters' Consultant id; null for the Super Admin's part.</summary>
    public Guid? ConsultantId { get; private set; }

    public InvoiceDiscountRequestStatus Status { get; private set; }
    public Guid? DecidedBy { get; private set; }
    public string? DecidedByName { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public string? DecisionNotes { get; private set; }

    /// <summary>A Super Admin decided this consultant's part for them (the consultant has no
    /// login, or isn't available).</summary>
    public bool DecidedOnBehalf { get; private set; }

    // Required by EF Core materialization.
    private InvoiceDiscountApproval()
    {
    }

    private InvoiceDiscountApproval(Guid id, Guid requestId, Guid? consultantId, Guid? createdBy)
        : base(id, createdBy)
    {
        RequestId = requestId;
        ConsultantId = consultantId;
        ApproverType = consultantId is null ? InvoiceDiscountApproverType.SuperAdmin : InvoiceDiscountApproverType.Consultant;
        Status = InvoiceDiscountRequestStatus.Pending;
    }

    public static InvoiceDiscountApproval Create(Guid requestId, Guid? consultantId, Guid? createdBy)
        => new(Guid.CreateVersion7(), requestId, consultantId, createdBy);

    internal void Decide(InvoiceDiscountRequestStatus status, Guid? actorId, string? actorName, string? notes, bool onBehalf)
    {
        Status = status;
        DecidedBy = actorId;
        DecidedByName = string.IsNullOrWhiteSpace(actorName) ? null : actorName.Trim();
        DecidedAt = DateTime.UtcNow;
        DecisionNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        DecidedOnBehalf = onBehalf;
        MarkUpdated(actorId);
    }
}

/// <summary>One bill line's share of an <see cref="InvoiceDiscountRequest"/>, with the line's
/// identity and charge copied from <see cref="InvoiceLineItem"/> when the request was raised.</summary>
internal class InvoiceDiscountRequestLine : Entity
{
    public Guid RequestId { get; private set; }

    /// <summary>The <see cref="InvoiceDiscountApproval"/> part that decides this line.</summary>
    public Guid ApprovalId { get; private set; }

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

    private InvoiceDiscountRequestLine(Guid id, Guid requestId, Guid approvalId, InvoiceLineItem line, decimal discount, Guid? createdBy)
        : base(id, createdBy)
    {
        RequestId = requestId;
        ApprovalId = approvalId;
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

    public static InvoiceDiscountRequestLine Create(Guid requestId, Guid approvalId, InvoiceLineItem line, decimal discount, Guid? createdBy)
        => new(Guid.CreateVersion7(), requestId, approvalId, line, discount, createdBy);
}
