using HMS.Modules.Laboratory.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Laboratory.Domain;

/// <summary>One item to add, passed into <see cref="LabOrder.Create"/> — the Application
/// layer's shape for a not-yet-persisted <see cref="LabOrderItem"/>. Internal like the rest of
/// Domain/Application: Contracts' own CreateLabOrderLineRequest is the public-facing shape
/// LabOrderService maps from (expanding a package into one LabOrderItemSpec per resolved
/// service first). InvoiceLineItemId isn't part of the spec shape the handbook's Invoice.cs
/// precedent names explicitly, but LabOrderItem's own InvoiceLineItemId field requires it —
/// it's carried here since LabOrder.Create/LabOrderItem.Create is the only place items are
/// ever constructed.</summary>
internal sealed record LabOrderItemSpec(
    Guid ServiceId,
    Guid? PackageId,
    string TestName,
    Guid InvoiceLineItemId,
    Guid? DepartmentId,
    Guid? ConsultantId,
    LabSampleType? SampleType);

/// <summary>
/// The lab request for one Invoice — created once, in-process, when Billing's
/// InvoiceService.CreateAsync successfully persists an invoice containing at least one
/// BillingType.Laboratory line item (see LabOrderService.CreateFromInvoiceAsync's
/// idempotency check: a retried call for the same InvoiceId returns the existing order rather
/// than duplicating). PatientName/PatientUhid/Source are snapshotted at creation rather than a
/// live join, same rationale as Billing's own Invoice.PatientName/PatientUhid (an order should
/// keep showing who/what it was raised for even if the patient record or visit is edited
/// later). OverallStatus is computed, not stored — see its own doc comment for the precedence
/// ladder.
/// </summary>
internal class LabOrder : Entity
{
    public string LabOrderNumber { get; private set; } = null!;
    public Guid InvoiceId { get; private set; }
    public Guid PatientId { get; private set; }
    public string PatientName { get; private set; } = null!;
    public string PatientUhid { get; private set; } = null!;
    public Guid VisitId { get; private set; }

    /// <summary>Plain-string snapshot of whatever VisitType Billing resolved at order-creation
    /// time (e.g. "OP"/"IP") — not a separate Source enum; null when the visit lookup failed.</summary>
    public string? Source { get; private set; }

    public LabOrderPriority Priority { get; private set; } = LabOrderPriority.Routine;

    public DateTime? ReportGeneratedAt { get; private set; }
    public Guid? ReportGeneratedBy { get; private set; }
    public DateTime? ReportReleasedAt { get; private set; }
    public Guid? ReportReleasedBy { get; private set; }

    private readonly List<LabOrderItem> _items = [];
    public IReadOnlyCollection<LabOrderItem> Items => _items.AsReadOnly();

    /// <summary>Derived, never stored — see the precedence ladder below. Every branch is
    /// evaluated top-to-bottom, first match wins.</summary>
    public LabOrderStatus OverallStatus
    {
        get
        {
            if (ReportReleasedAt is not null)
            {
                return LabOrderStatus.Released;
            }

            if (ReportGeneratedAt is not null)
            {
                return LabOrderStatus.ReadyForRelease;
            }

            // Shouldn't happen in practice — Create requires at least one item — but a
            // computed property must still degrade gracefully rather than throw.
            if (_items.Count == 0)
            {
                return LabOrderStatus.PendingCollection;
            }

            if (_items.Any(i => i.Status == LabOrderItemStatus.RecollectionRequired))
            {
                return LabOrderStatus.RecollectionRequired;
            }

            if (_items.Any(i => i.Status == LabOrderItemStatus.Rejected))
            {
                return LabOrderStatus.Rejected;
            }

            if (_items.Any(i => i.Status == LabOrderItemStatus.CorrectionRequired))
            {
                return LabOrderStatus.CorrectionRequired;
            }

            if (_items.All(i => i.Status == LabOrderItemStatus.Verified))
            {
                return LabOrderStatus.Verified;
            }

            if (_items.Any(i => i.Status == LabOrderItemStatus.PendingVerification))
            {
                return LabOrderStatus.PendingVerification;
            }

            if (_items.Any(i => i.Status == LabOrderItemStatus.ResultEntryInProgress))
            {
                return LabOrderStatus.ResultEntryInProgress;
            }

            if (_items.Any(i => i.Status == LabOrderItemStatus.Processing))
            {
                return LabOrderStatus.Processing;
            }

            if (_items.All(i => i.Status != LabOrderItemStatus.PendingCollection && i.Status != LabOrderItemStatus.Collected))
            {
                return LabOrderStatus.Received;
            }

            if (_items.Any(i => i.Status == LabOrderItemStatus.Collected))
            {
                return LabOrderStatus.Collected;
            }

            return LabOrderStatus.PendingCollection;
        }
    }

    // Required by EF Core materialization.
    private LabOrder()
    {
    }

    private LabOrder(
        Guid id,
        string labOrderNumber,
        Guid invoiceId,
        Guid patientId,
        string patientName,
        string patientUhid,
        Guid visitId,
        string? source,
        Guid? createdBy)
        : base(id, createdBy)
    {
        LabOrderNumber = labOrderNumber;
        InvoiceId = invoiceId;
        PatientId = patientId;
        PatientName = patientName;
        PatientUhid = patientUhid;
        VisitId = visitId;
        Source = source;
    }

    /// <summary>Builds the order and every one of its items in one call — mirrors Invoice.Create.
    /// Rejects an empty item list (LabOrderService is responsible for returning a proper
    /// Result.Failure(EmptyOrder) before ever calling this, same split as Invoice/
    /// CreateInvoiceRequestValidator's "reject empty invoice" precedent).</summary>
    public static LabOrder Create(
        string labOrderNumber,
        Guid invoiceId,
        Guid patientId,
        string patientName,
        string patientUhid,
        Guid visitId,
        string? source,
        IReadOnlyList<LabOrderItemSpec> items,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(labOrderNumber, nameof(labOrderNumber));
        Guard.AgainstNullOrWhiteSpace(patientName, nameof(patientName));
        Guard.AgainstNullOrWhiteSpace(patientUhid, nameof(patientUhid));
        if (items.Count == 0)
        {
            throw new ArgumentException("A lab order must have at least one item.", nameof(items));
        }

        var order = new LabOrder(
            Guid.CreateVersion7(),
            labOrderNumber.Trim(),
            invoiceId,
            patientId,
            patientName.Trim(),
            patientUhid.Trim(),
            visitId,
            string.IsNullOrWhiteSpace(source) ? null : source.Trim(),
            createdBy);

        foreach (var spec in items)
        {
            order._items.Add(LabOrderItem.Create(order.Id, spec, createdBy));
        }

        return order;
    }

    /// <summary>No endpoint sets this at creation time yet — added for the worklist's own
    /// triage use later.</summary>
    public void UpdatePriority(LabOrderPriority priority, Guid? updatedBy)
    {
        Priority = priority;
        MarkUpdated(updatedBy);
    }

    /// <summary>Caller (LabOrderService) pre-checks the same precondition to return a proper
    /// Result.Failure(NotAllItemsVerified) with a clear message; repeated here since it's a
    /// genuine invariant of a report-generated LabOrder, not just an HTTP-layer concern —
    /// same split as Invoice.Void's own doc comment.</summary>
    public void GenerateReport(Guid? actorId)
    {
        if (_items.Count == 0 || !_items.All(i => i.Status == LabOrderItemStatus.Verified))
        {
            throw new InvalidOperationException("Every item on this order must be Verified before a report can be generated.");
        }

        ReportGeneratedAt = DateTime.UtcNow;
        ReportGeneratedBy = actorId;
        MarkUpdated(actorId);
    }

    /// <summary>See GenerateReport's doc comment on the pre-check/domain-guard split.</summary>
    public void ReleaseReport(Guid? actorId)
    {
        if (ReportGeneratedAt is null)
        {
            throw new InvalidOperationException("The report must be generated before it can be released.");
        }

        if (ReportReleasedAt is not null)
        {
            throw new InvalidOperationException("This order's report has already been released.");
        }

        ReportReleasedAt = DateTime.UtcNow;
        ReportReleasedBy = actorId;
        MarkUpdated(actorId);
    }
}
