using HMS.Modules.Laboratory.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Laboratory.Domain;

/// <summary>One result-parameter row to save, passed into <see cref="LabOrderItem.SaveResultDraft"/>
/// — the Application layer's shape for a not-yet-persisted <see cref="LabResultParameter"/>.
/// Mirrors LabOrderItemSpec/InvoiceLineItemSpec's own role.</summary>
internal sealed record LabResultParameterSpec(
    string ParameterName,
    string ResultValue,
    string? Unit,
    string? ReferenceRange,
    LabResultFlag? Flag,
    string? Remarks);

/// <summary>
/// One ordered test on a LabOrder — whether billed standalone or expanded from a billed
/// package (in which case several items share the same PackageId and InvoiceLineItemId).
/// ServiceId/DepartmentId/ConsultantId are app-level references into Masters/Patients, no DB
/// FK, same convention as every other cross-module reference in this codebase. TestName is
/// snapshotted from DiagnosticService.Name at creation so this row keeps reading correctly
/// even if the master catalog entry is renamed later.
/// </summary>
internal class LabOrderItem : Entity
{
    public Guid LabOrderId { get; private set; }
    public Guid ServiceId { get; private set; }
    public Guid? PackageId { get; private set; }
    public Guid InvoiceLineItemId { get; private set; }
    public string TestName { get; private set; } = null!;
    public Guid? DepartmentId { get; private set; }
    public Guid? ConsultantId { get; private set; }
    public LabSampleType? SampleType { get; private set; }
    public LabOrderItemStatus Status { get; private set; } = LabOrderItemStatus.PendingCollection;

    public DateTime? CollectedAt { get; private set; }
    public Guid? CollectedBy { get; private set; }
    public string? CollectionLocation { get; private set; }
    public string? SampleQuantity { get; private set; }
    public string? CollectionRemarks { get; private set; }

    public LabSampleRejectionReason? RejectionReason { get; private set; }
    public string? RejectionRemarks { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public Guid? RejectedBy { get; private set; }

    public DateTime? VerifiedAt { get; private set; }
    public Guid? VerifiedBy { get; private set; }
    public string? CorrectionReason { get; private set; }
    public DateTime? CorrectionRequestedAt { get; private set; }
    public Guid? CorrectionRequestedBy { get; private set; }

    public DateTime? SubmittedForVerificationAt { get; private set; }
    public Guid? SubmittedForVerificationBy { get; private set; }

    private readonly List<LabResultParameter> _parameters = [];
    public IReadOnlyCollection<LabResultParameter> Parameters => _parameters.AsReadOnly();

    private readonly List<LabOrderItemEvent> _events = [];
    public IReadOnlyCollection<LabOrderItemEvent> Events => _events.AsReadOnly();

    // Required by EF Core materialization.
    private LabOrderItem()
    {
    }

    private LabOrderItem(
        Guid id,
        Guid labOrderId,
        Guid serviceId,
        Guid? packageId,
        Guid invoiceLineItemId,
        string testName,
        Guid? departmentId,
        Guid? consultantId,
        Guid? createdBy)
        : base(id, createdBy)
    {
        LabOrderId = labOrderId;
        ServiceId = serviceId;
        PackageId = packageId;
        InvoiceLineItemId = invoiceLineItemId;
        TestName = testName;
        DepartmentId = departmentId;
        ConsultantId = consultantId;
    }

    internal static LabOrderItem Create(Guid labOrderId, LabOrderItemSpec spec, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(spec.TestName, nameof(spec.TestName));

        var item = new LabOrderItem(
            Guid.CreateVersion7(),
            labOrderId,
            spec.ServiceId,
            spec.PackageId,
            spec.InvoiceLineItemId,
            spec.TestName.Trim(),
            spec.DepartmentId,
            spec.ConsultantId,
            createdBy);

        if (spec.SampleType.HasValue)
        {
            item.SampleType = spec.SampleType;
        }

        item._events.Add(LabOrderItemEvent.Create(item.Id, LabOrderItemEventType.Created, createdBy, null));

        return item;
    }

    /// <summary>Valid only from PendingCollection or RecollectionRequired. Clears any prior
    /// rejection fields — a fresh collection attempt supersedes the earlier rejection.</summary>
    public void CollectSample(LabSampleType sampleType, string? location, string? quantity, string? remarks, Guid? actorId)
    {
        if (Status is not (LabOrderItemStatus.PendingCollection or LabOrderItemStatus.RecollectionRequired))
        {
            throw new InvalidOperationException($"Cannot collect a sample while the item is '{Status}'.");
        }

        Status = LabOrderItemStatus.Collected;
        SampleType = sampleType;
        CollectedAt = DateTime.UtcNow;
        CollectedBy = actorId;
        CollectionLocation = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
        SampleQuantity = string.IsNullOrWhiteSpace(quantity) ? null : quantity.Trim();
        CollectionRemarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();

        RejectionReason = null;
        RejectionRemarks = null;
        RejectedAt = null;
        RejectedBy = null;

        MarkUpdated(actorId);
        _events.Add(LabOrderItemEvent.Create(Id, LabOrderItemEventType.SampleCollected, actorId, remarks));
    }

    /// <summary>Valid only from Collected.</summary>
    public void RejectSample(LabSampleRejectionReason reason, string? remarks, Guid? actorId)
    {
        if (Status != LabOrderItemStatus.Collected)
        {
            throw new InvalidOperationException($"Cannot reject a sample while the item is '{Status}'.");
        }

        Status = LabOrderItemStatus.Rejected;
        RejectionReason = reason;
        RejectionRemarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();
        RejectedAt = DateTime.UtcNow;
        RejectedBy = actorId;

        MarkUpdated(actorId);
        _events.Add(LabOrderItemEvent.Create(Id, LabOrderItemEventType.SampleRejected, actorId, remarks));
    }

    /// <summary>Valid only from Rejected.</summary>
    public void RequestRecollection(Guid? actorId)
    {
        if (Status != LabOrderItemStatus.Rejected)
        {
            throw new InvalidOperationException($"Cannot request a recollection while the item is '{Status}'.");
        }

        Status = LabOrderItemStatus.RecollectionRequired;

        MarkUpdated(actorId);
        _events.Add(LabOrderItemEvent.Create(Id, LabOrderItemEventType.RecollectionRequested, actorId, null));
    }

    /// <summary>Valid only from Collected.</summary>
    public void ReceiveSample(Guid? actorId)
    {
        if (Status != LabOrderItemStatus.Collected)
        {
            throw new InvalidOperationException($"Cannot receive a sample while the item is '{Status}'.");
        }

        Status = LabOrderItemStatus.Received;

        MarkUpdated(actorId);
        _events.Add(LabOrderItemEvent.Create(Id, LabOrderItemEventType.SampleReceived, actorId, null));
    }

    /// <summary>Valid only from Received or CorrectionRequired (the correction-flow re-entry
    /// point back into processing).</summary>
    public void StartProcessing(Guid? actorId)
    {
        if (Status is not (LabOrderItemStatus.Received or LabOrderItemStatus.CorrectionRequired))
        {
            throw new InvalidOperationException($"Cannot start processing while the item is '{Status}'.");
        }

        Status = LabOrderItemStatus.Processing;

        MarkUpdated(actorId);
        _events.Add(LabOrderItemEvent.Create(Id, LabOrderItemEventType.ProcessingStarted, actorId, null));
    }

    /// <summary>Valid from Processing, ResultEntryInProgress, or CorrectionRequired. Replaces
    /// the full parameter set (clear + re-add) each time — matches how a technician actually
    /// fills out a result form in one sitting, mirroring how Invoice.Create builds all its
    /// InvoiceLineItems in one call rather than incrementally.</summary>
    public void SaveResultDraft(IReadOnlyList<LabResultParameterSpec> parameters, Guid? actorId)
    {
        if (Status is not (LabOrderItemStatus.Processing or LabOrderItemStatus.ResultEntryInProgress or LabOrderItemStatus.CorrectionRequired))
        {
            throw new InvalidOperationException($"Cannot save result parameters while the item is '{Status}'.");
        }

        _parameters.Clear();
        foreach (var spec in parameters)
        {
            _parameters.Add(LabResultParameter.Create(Id, spec.ParameterName, spec.ResultValue, spec.Unit, spec.ReferenceRange, spec.Flag, spec.Remarks, actorId));
        }

        Status = LabOrderItemStatus.ResultEntryInProgress;

        MarkUpdated(actorId);
        _events.Add(LabOrderItemEvent.Create(Id, LabOrderItemEventType.ResultDraftSaved, actorId, null));
    }

    /// <summary>Valid only from ResultEntryInProgress; requires at least one saved parameter.</summary>
    public void SubmitForVerification(Guid? actorId)
    {
        if (Status != LabOrderItemStatus.ResultEntryInProgress)
        {
            throw new InvalidOperationException($"Cannot submit for verification while the item is '{Status}'.");
        }

        if (_parameters.Count == 0)
        {
            throw new InvalidOperationException("At least one result parameter is required before submitting for verification.");
        }

        Status = LabOrderItemStatus.PendingVerification;
        SubmittedForVerificationAt = DateTime.UtcNow;
        SubmittedForVerificationBy = actorId;

        MarkUpdated(actorId);
        _events.Add(LabOrderItemEvent.Create(Id, LabOrderItemEventType.SubmittedForVerification, actorId, null));
    }

    /// <summary>Valid only from PendingVerification.</summary>
    public void Verify(Guid? actorId)
    {
        if (Status != LabOrderItemStatus.PendingVerification)
        {
            throw new InvalidOperationException($"Cannot verify while the item is '{Status}'.");
        }

        Status = LabOrderItemStatus.Verified;
        VerifiedAt = DateTime.UtcNow;
        VerifiedBy = actorId;

        MarkUpdated(actorId);
        _events.Add(LabOrderItemEvent.Create(Id, LabOrderItemEventType.Verified, actorId, null));
    }

    /// <summary>Valid only from PendingVerification.</summary>
    public void RejectForCorrection(string reason, Guid? actorId)
    {
        Guard.AgainstNullOrWhiteSpace(reason, nameof(reason));
        if (Status != LabOrderItemStatus.PendingVerification)
        {
            throw new InvalidOperationException($"Cannot request a correction while the item is '{Status}'.");
        }

        Status = LabOrderItemStatus.CorrectionRequired;
        CorrectionReason = reason.Trim();
        CorrectionRequestedAt = DateTime.UtcNow;
        CorrectionRequestedBy = actorId;

        MarkUpdated(actorId);
        _events.Add(LabOrderItemEvent.Create(Id, LabOrderItemEventType.CorrectionRequested, actorId, reason));
    }
}
