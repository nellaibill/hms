namespace HMS.Modules.Laboratory.Contracts;

/// <summary>How urgently a LabOrder's samples/results should be handled. Defaults to Routine —
/// no endpoint sets this at order-creation time yet (see Domain/LabOrder.cs's UpdatePriority),
/// it exists for the worklist's own triage use later.</summary>
public enum LabOrderPriority
{
    Routine = 0,
    Urgent = 1,
    Stat = 2,
}

/// <summary>Per-item state machine — see Domain/LabOrderItem.cs's mutators for the exact legal
/// transitions between these. LabOrder's own OverallStatus (see LabOrderStatus below) is
/// derived from every item's Status, not stored separately.</summary>
public enum LabOrderItemStatus
{
    PendingCollection,
    Collected,
    Received,
    Processing,
    ResultEntryInProgress,
    PendingVerification,
    CorrectionRequired,
    Verified,
    Rejected,
    RecollectionRequired,
}

/// <summary>The computed, order-level status shown on the worklist — never stored, always
/// derived from every LabOrderItem's own Status plus the order's ReportGeneratedAt/
/// ReportReleasedAt timestamps. See Domain/LabOrder.cs's OverallStatus getter for the exact
/// precedence ladder. Shares most value names with LabOrderItemStatus (same underlying
/// vocabulary at both granularities) plus two order-only states for the reporting milestones.</summary>
public enum LabOrderStatus
{
    PendingCollection,
    Collected,
    Received,
    Processing,
    ResultEntryInProgress,
    PendingVerification,
    CorrectionRequired,
    Verified,
    Rejected,
    RecollectionRequired,
    ReadyForRelease,
    Released,
}

/// <summary>Set/confirmed by the technician during sample collection — unset until then.</summary>
public enum LabSampleType
{
    Blood,
    Urine,
    Stool,
    Sputum,
    Swab,
    Serum,
    Plasma,
    Other,
}

/// <summary>Why a collected sample was rejected — set only by RejectSample.</summary>
public enum LabSampleRejectionReason
{
    InsufficientSample,
    IncorrectSample,
    DamagedSample,
    HemolyzedSample,
    WrongLabel,
    ContaminatedSample,
    Other,
}

/// <summary>A result parameter's abnormality flag — only ever set by explicit human/API input
/// (see Domain/LabResultParameter.cs), never auto-computed: this system has no reference-range
/// configuration data to compute it safely from.</summary>
public enum LabResultFlag
{
    Normal,
    High,
    Low,
    Critical,
    Abnormal,
}

/// <summary>One append-only audit/history entry on a LabOrderItem — see
/// Domain/LabOrderItemEvent.cs. Doubles as both sample-status history and general audit trail.</summary>
public enum LabOrderItemEventType
{
    Created,
    SampleCollected,
    SampleRejected,
    RecollectionRequested,
    SampleReceived,
    ProcessingStarted,
    ResultDraftSaved,
    SubmittedForVerification,
    Verified,
    CorrectionRequested,
}
