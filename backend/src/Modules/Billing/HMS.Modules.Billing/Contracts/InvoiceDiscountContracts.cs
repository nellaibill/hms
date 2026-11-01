namespace HMS.Modules.Billing.Contracts;

/// <summary>Lifecycle of a bill discount request (ADR-109) and of each of its approval parts
/// (ADR-111). A request is Pending while any part waits; once every part is decided it is
/// Approved (all approved), Rejected (all rejected) or PartlyApproved (a mix — a part itself is
/// never PartlyApproved). Cancelled when the bill is voided while still waiting. Also the
/// invoice's own <see cref="InvoiceResponse.DiscountStatus"/>, which tracks its latest request.</summary>
public enum InvoiceDiscountRequestStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
    PartlyApproved,
}

/// <summary>Who decides one part of a discount request (ADR-111): the consultant whose
/// consultation fee is discounted, or the Super Admin for every other line.</summary>
public enum InvoiceDiscountApproverType
{
    SuperAdmin,
    Consultant,
}

/// <summary>How the counter entered the discount — kept for display only; the per-line amounts
/// are what is applied.</summary>
public enum InvoiceDiscountType
{
    Amount,
    Percent,
}

/// <summary>Who is making a billing change. Built by the controller from the caller's token:
/// a Super Admin's own discount needs no approval, anyone else's waits for one.
/// <paramref name="ConsultantId"/> is set only for a consultant's own login (a linked consultant
/// and a Consultant role — ClaimsPrincipalExtensions.GetScopedConsultantId): it lets them decide
/// the discount parts on their own fees (ADR-111).</summary>
public sealed record BillingActor(Guid? UserId, string? UserName, bool IsSuperAdmin, Guid? ConsultantId = null);

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

    /// <summary>The <see cref="InvoiceDiscountApprovalResponse"/> this line belongs to.</summary>
    public Guid ApprovalId { get; init; }
}

/// <summary>One approver's part of a discount request (ADR-111) — the lines they decide and
/// their decision.</summary>
public record InvoiceDiscountApprovalResponse
{
    public Guid Id { get; init; }
    public InvoiceDiscountApproverType ApproverType { get; init; }

    /// <summary>The consultant who decides this part; null for the Super Admin's part.</summary>
    public Guid? ConsultantId { get; init; }

    /// <summary>Pending, Approved, Rejected or Cancelled.</summary>
    public InvoiceDiscountRequestStatus Status { get; init; }

    /// <summary>Sum of this part's lines' discounts.</summary>
    public decimal TotalDiscount { get; init; }

    public Guid? DecidedBy { get; init; }
    public string? DecidedByName { get; init; }
    public DateTime? DecidedAt { get; init; }
    public string? DecisionNotes { get; init; }

    /// <summary>True when a Super Admin decided a consultant's part for them.</summary>
    public bool DecidedOnBehalf { get; init; }
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

    /// <summary>Sum of the discounts in approved parts — what actually came off the bill.</summary>
    public decimal ApprovedDiscount { get; init; }

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

    /// <summary>One part per approver; the Super Admin's part (if any) comes last.</summary>
    public IReadOnlyList<InvoiceDiscountApprovalResponse> Approvals { get; init; } = [];
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

/// <summary>A reason this hospital has given before (for a discount or a bill revert), offered as
/// a suggestion when the counter types one — most-used first.</summary>
public record ReasonSuggestionResponse
{
    public string Reason { get; init; } = string.Empty;

    /// <summary>How many of the recent requests gave this reason (ignoring case and spacing).</summary>
    public int TimesUsed { get; init; }
}

/// <summary>Standard paging shape (docs/ApiStandards.md §6) plus a status filter — on the
/// request for the Super Admin's list, on the caller's own part for a consultant's list.</summary>
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
