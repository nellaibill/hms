namespace HMS.Modules.Billing.Contracts;

/// <summary>Lifecycle of a bill discount request (ADR-109) — raised Pending by billing staff,
/// then decided once by a Super Admin. Cancelled when the bill is voided while still waiting.
/// Also the invoice's own <see cref="InvoiceResponse.DiscountStatus"/>, which tracks its latest
/// request.</summary>
public enum InvoiceDiscountRequestStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
}

/// <summary>How the counter entered the discount — kept for display only; the per-line amounts
/// are what is applied.</summary>
public enum InvoiceDiscountType
{
    Amount,
    Percent,
}

/// <summary>Who is making a billing change. Built by the controller from the caller's token:
/// a Super Admin's own discount needs no approval, anyone else's waits for one.</summary>
public sealed record BillingActor(Guid? UserId, string? UserName, bool IsSuperAdmin);

/// <summary>One bill line's share of a discount request.</summary>
public record InvoiceDiscountLineRequest
{
    public Guid LineItemId { get; init; }
    public decimal Discount { get; init; }
}

/// <summary>Asks for a discount on lines of an existing, unpaid bill — a reason is required,
/// since it is what the Super Admin decides on.</summary>
public record CreateInvoiceDiscountRequest
{
    public InvoiceDiscountType DiscountType { get; init; }

    /// <summary>The ₹ amount or % the counter typed (see <see cref="DiscountType"/>).</summary>
    public decimal DiscountValue { get; init; }

    public string Reason { get; init; } = string.Empty;
    public IReadOnlyList<InvoiceDiscountLineRequest> Lines { get; init; } = [];
}

public record ApproveInvoiceDiscountRequest
{
    public string? Notes { get; init; }
}

/// <summary>Notes are required for a rejection, so the counter knows why.</summary>
public record RejectInvoiceDiscountRequest
{
    public string Notes { get; init; } = string.Empty;
}

/// <summary>One discounted line, snapshotted from the bill line when the request was raised so
/// the approval list can name the service without loading the invoice.</summary>
public record InvoiceDiscountRequestLineResponse
{
    public Guid LineItemId { get; init; }
    public BillingType BillingType { get; init; }
    public string? DepartmentId { get; init; }
    public string? ConsultantId { get; init; }
    public string? ServiceId { get; init; }
    public Guid? PackageId { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }

    /// <summary>Quantity × UnitPrice — the line's charge before this discount.</summary>
    public decimal LineAmount { get; init; }

    public decimal Discount { get; init; }
}

public record InvoiceDiscountRequestResponse
{
    public Guid Id { get; init; }
    public Guid InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public Guid PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string PatientUhid { get; init; } = string.Empty;

    /// <summary>The whole bill's gross amount when the request was raised.</summary>
    public decimal GrossAmount { get; init; }

    public InvoiceDiscountType DiscountType { get; init; }
    public decimal DiscountValue { get; init; }

    /// <summary>Sum of <see cref="Lines"/>' discounts.</summary>
    public decimal TotalDiscount { get; init; }

    public string Reason { get; init; } = string.Empty;
    public InvoiceDiscountRequestStatus Status { get; init; }
    public Guid? RequestedBy { get; init; }
    public string? RequestedByName { get; init; }
    public DateTime RequestedAt { get; init; }
    public Guid? DecidedBy { get; init; }
    public string? DecidedByName { get; init; }
    public DateTime? DecidedAt { get; init; }
    public string? DecisionNotes { get; init; }
    public IReadOnlyList<InvoiceDiscountRequestLineResponse> Lines { get; init; } = [];
}

/// <summary>The discount state of one invoice — backs the invoice page's "Request Discount"
/// button and status banner.</summary>
public record InvoiceDiscountStatusResponse
{
    /// <summary>True when a new discount request may be raised on this invoice right now.</summary>
    public bool CanRequest { get; init; }

    /// <summary>Why <see cref="CanRequest"/> is false; null when it's true.</summary>
    public string? IneligibleReason { get; init; }

    /// <summary>Every discount request raised on this invoice, newest first.</summary>
    public IReadOnlyList<InvoiceDiscountRequestResponse> Requests { get; init; } = [];
}

/// <summary>Standard paging shape (docs/ApiStandards.md §6) plus a status filter.</summary>
public record InvoiceDiscountRequestListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public InvoiceDiscountRequestStatus? Status { get; init; }
}

/// <summary>Collects everything still owed on a bill in one go — the same method/amount rows as
/// <see cref="CreateInvoiceRequest.Payments"/>, applied to the unpaid lines only. Empty is
/// allowed only when nothing is owed (a fully discounted bill).</summary>
public record PayInvoiceRequest
{
    public IReadOnlyList<CreateInvoicePaymentRequest> Payments { get; init; } = [];
}
