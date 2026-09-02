namespace HMS.Modules.Laboratory.Contracts;

/// <summary>One line Billing supplies per BillingType.Laboratory invoice line item — either
/// ServiceId (a standalone test) or PackageId (a package this module expands into its own
/// items via IDiagnosticPackageService) is set, never both/neither (validated by
/// LabOrderService, not a request validator, since the check depends on resolving the
/// referenced Masters record — see LabOrderService.CreateFromInvoiceAsync).</summary>
public record CreateLabOrderLineRequest
{
    public Guid InvoiceLineItemId { get; init; }
    public Guid? ServiceId { get; init; }
    public Guid? PackageId { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? ConsultantId { get; init; }
}

/// <summary>The one public entry point Billing calls, in-process, right after an invoice
/// containing at least one Laboratory line item is persisted — see
/// Application/LabOrderService.cs's CreateFromInvoiceAsync for the idempotency/expansion
/// logic. There is deliberately no HTTP endpoint for this — lab staff never manually create a
/// patient billing request, only Billing does, in-process.</summary>
public record CreateLabOrderFromInvoiceRequest
{
    public Guid InvoiceId { get; init; }
    public Guid PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string PatientUhid { get; init; } = string.Empty;
    public Guid VisitId { get; init; }

    /// <summary>Snapshot of the visit's own VisitType (as a plain string, e.g. "OP"/"IP") at
    /// order-creation time — see Domain/LabOrder.cs's Source field. Null when the visit type
    /// could not be resolved.</summary>
    public string? Source { get; init; }

    public IReadOnlyList<CreateLabOrderLineRequest> Lines { get; init; } = [];
}

public record CollectSampleRequest
{
    public LabSampleType SampleType { get; init; }
    public string? Location { get; init; }
    public string? Quantity { get; init; }
    public string? Remarks { get; init; }
}

public record RejectSampleRequest
{
    public LabSampleRejectionReason Reason { get; init; }
    public string? Remarks { get; init; }
}

public record ResultParameterRequest
{
    public string ParameterName { get; init; } = string.Empty;
    public string ResultValue { get; init; } = string.Empty;
    public string? Unit { get; init; }
    public string? ReferenceRange { get; init; }
    public LabResultFlag? Flag { get; init; }
    public string? Remarks { get; init; }
}

/// <summary>Replaces the item's full parameter set — see Domain/LabOrderItem.cs's
/// SaveResultDraft for why a full replace (not incremental add/edit/remove) matches how a
/// technician actually fills out a result form in one sitting.</summary>
public record SaveResultDraftRequest
{
    public IReadOnlyList<ResultParameterRequest> Parameters { get; init; } = [];
}

public record RejectForCorrectionRequest
{
    public string Reason { get; init; } = string.Empty;
}

/// <summary>Standard paging/sort/search shape (docs/ApiStandards.md §6), plus the worklist's
/// own status/priority/date-range filters — mirrors Billing's InvoiceListQuery.</summary>
public record LabOrderListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Sort { get; init; }
    public string? Search { get; init; }
    public LabOrderStatus? Status { get; init; }
    public LabOrderPriority? Priority { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
}

public record ResultParameterResponse
{
    public Guid Id { get; init; }
    public string ParameterName { get; init; } = string.Empty;
    public string ResultValue { get; init; } = string.Empty;
    public string? Unit { get; init; }
    public string? ReferenceRange { get; init; }
    public LabResultFlag? Flag { get; init; }
    public string? Remarks { get; init; }
}

public record LabOrderItemEventResponse
{
    public Guid Id { get; init; }
    public LabOrderItemEventType EventType { get; init; }
    public Guid? ActorId { get; init; }
    public DateTime OccurredAt { get; init; }
    public string? Remarks { get; init; }
}

public record LabOrderItemResponse
{
    public Guid Id { get; init; }
    public Guid ServiceId { get; init; }
    public Guid? PackageId { get; init; }
    public string TestName { get; init; } = string.Empty;
    public Guid? DepartmentId { get; init; }
    public Guid? ConsultantId { get; init; }
    public LabSampleType? SampleType { get; init; }
    public LabOrderItemStatus Status { get; init; }

    public DateTime? CollectedAt { get; init; }
    public Guid? CollectedBy { get; init; }
    public string? CollectionLocation { get; init; }
    public string? SampleQuantity { get; init; }
    public string? CollectionRemarks { get; init; }

    public LabSampleRejectionReason? RejectionReason { get; init; }
    public string? RejectionRemarks { get; init; }
    public DateTime? RejectedAt { get; init; }
    public Guid? RejectedBy { get; init; }

    public DateTime? SubmittedForVerificationAt { get; init; }
    public DateTime? VerifiedAt { get; init; }
    public Guid? VerifiedBy { get; init; }
    public string? CorrectionReason { get; init; }
    public DateTime? CorrectionRequestedAt { get; init; }

    public IReadOnlyList<ResultParameterResponse> Parameters { get; init; } = [];
    public IReadOnlyList<LabOrderItemEventResponse> Events { get; init; } = [];
}

public record LabOrderResponse
{
    public Guid Id { get; init; }
    public string LabOrderNumber { get; init; } = string.Empty;
    public Guid InvoiceId { get; init; }
    public Guid PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string PatientUhid { get; init; } = string.Empty;
    public Guid VisitId { get; init; }
    public string? Source { get; init; }
    public LabOrderPriority Priority { get; init; }
    public LabOrderStatus OverallStatus { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ReportGeneratedAt { get; init; }
    public Guid? ReportGeneratedBy { get; init; }
    public DateTime? ReportReleasedAt { get; init; }
    public Guid? ReportReleasedBy { get; init; }
    public IReadOnlyList<LabOrderItemResponse> Items { get; init; } = [];
}

/// <summary>Backs the lab worklist dashboard's summary tiles — computed server-side over the
/// current tenant's LabOrder/LabOrderItem rows. "Today" = CreatedAt's date matches server UTC
/// today, matching this repo's existing UTC convention.</summary>
public record LabDashboardSummaryResponse
{
    public int TotalRequestsToday { get; init; }
    public int PendingSampleCollection { get; init; }
    public int SamplesCollected { get; init; }
    public int SamplesReceived { get; init; }
    public int TestsInProgress { get; init; }
    public int ResultsPendingEntry { get; init; }
    public int PendingVerification { get; init; }
    public int ReportsReady { get; init; }
    public int ReportsReleased { get; init; }
    public int RejectedOrRecollectionRequired { get; init; }
}
