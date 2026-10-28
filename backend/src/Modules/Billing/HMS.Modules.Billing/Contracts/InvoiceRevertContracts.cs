namespace HMS.Modules.Billing.Contracts;

/// <summary>Lifecycle of an OP bill revert request (ADR-106) — raised Pending by billing staff,
/// then decided once by a Super Admin.</summary>
public enum InvoiceRevertRequestStatus
{
    Pending,
    Approved,
    Rejected,
}

/// <summary>Asks a Super Admin to revert (cancel) an OP bill — the reason is required, since it
/// is what the Super Admin decides on and what the reverted invoice keeps as its void reason.</summary>
public record CreateInvoiceRevertRequest
{
    public string Reason { get; init; } = string.Empty;
}

public record ApproveInvoiceRevertRequest
{
    public string? Notes { get; init; }
}

/// <summary>Notes are required for a rejection, so the requester knows why.</summary>
public record RejectInvoiceRevertRequest
{
    public string Notes { get; init; } = string.Empty;
}

public record InvoiceRevertRequestResponse
{
    public Guid Id { get; init; }
    public Guid InvoiceId { get; init; }

    /// <summary>Invoice number, patient and amounts are snapshotted when the request is raised,
    /// so the Super Admin's list reads without loading every invoice.</summary>
    public string InvoiceNumber { get; init; } = string.Empty;
    public Guid PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string PatientUhid { get; init; } = string.Empty;
    public decimal NetAmount { get; init; }

    /// <summary>What had been collected on the bill when the request was raised — the amount the
    /// counter refunds once the revert is approved.</summary>
    public decimal PaidAmount { get; init; }

    public string Reason { get; init; } = string.Empty;
    public InvoiceRevertRequestStatus Status { get; init; }
    public Guid? RequestedBy { get; init; }
    public string? RequestedByName { get; init; }
    public DateTime RequestedAt { get; init; }
    public Guid? DecidedBy { get; init; }
    public string? DecidedByName { get; init; }
    public DateTime? DecidedAt { get; init; }
    public string? DecisionNotes { get; init; }
}

/// <summary>The revert state of one invoice — backs the invoice detail page's "Request Revert"
/// button and status banner.</summary>
public record InvoiceRevertStatusResponse
{
    /// <summary>True when a new revert request may be raised on this invoice right now.</summary>
    public bool CanRequest { get; init; }

    /// <summary>Why <see cref="CanRequest"/> is false (not an OP bill, already voided, a request
    /// already pending); null when it's true.</summary>
    public string? IneligibleReason { get; init; }

    /// <summary>Every revert request raised on this invoice, newest first.</summary>
    public IReadOnlyList<InvoiceRevertRequestResponse> Requests { get; init; } = [];
}

/// <summary>Standard paging shape (docs/ApiStandards.md §6) plus a status filter.</summary>
public record InvoiceRevertRequestListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public InvoiceRevertRequestStatus? Status { get; init; }
}
