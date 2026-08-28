namespace HMS.Modules.Billing.Contracts;

/// <summary>One priced line to create — mirrors frontend/web/src/features/billing/types.ts's
/// BillingItem, minus the fields the server derives (Id, PaymentStatus, Total).</summary>
public record CreateInvoiceLineItemRequest
{
    public BillingType BillingType { get; init; }

    /// <summary>Free-text department/consultant/service identifiers, not Masters-backed
    /// Guid FKs — see Domain/InvoiceLineItem.cs for why.</summary>
    public string? DepartmentId { get; init; }
    public string? ConsultantId { get; init; }
    public string? ServiceId { get; init; }

    /// <summary>App-level reference into Masters' DiagnosticPackage — set for a package line,
    /// null for a plain service line. See Domain/InvoiceLineItem.cs.</summary>
    public Guid? PackageId { get; init; }

    public int Quantity { get; init; } = 1;
    public decimal UnitPrice { get; init; }
    public decimal Discount { get; init; }
    public bool DiscountApproved { get; init; }
    public string? DiscountApprovedBy { get; init; }
}

/// <summary>
/// Creates a whole invoice in one call — matches the UI's actual use case (a bill is
/// entered and saved as one transaction at the counter, not built up field-by-field against
/// a persisted draft), the same way PatientRegistration's Billing step and the standalone
/// OPD Billing Entry screen both work today against the mock store.
/// </summary>
public record CreateInvoiceRequest
{
    public Guid PatientId { get; init; }

    /// <summary>References Patients' PatientRegistration.Id when a real encounter exists;
    /// falls back to PatientId itself for flows with no registration concept yet (e.g. OPD
    /// Billing Entry for a returning patient) — mirrors the frontend's own fallback.</summary>
    public Guid VisitId { get; init; }

    /// <summary>Snapshotted at creation, not a live join — see Domain/Invoice.cs.</summary>
    public string PatientName { get; init; } = string.Empty;
    public string PatientUhid { get; init; } = string.Empty;

    public IReadOnlyList<CreateInvoiceLineItemRequest> Items { get; init; } = [];
}

public record RecordPaymentRequest
{
    public PaymentMethod Method { get; init; }
}

public record VoidInvoiceRequest
{
    public string Reason { get; init; } = string.Empty;
}

public record InvoiceLineItemResponse
{
    public Guid Id { get; init; }
    public BillingType BillingType { get; init; }
    public string? DepartmentId { get; init; }
    public string? ConsultantId { get; init; }
    public string? ServiceId { get; init; }
    public Guid? PackageId { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Discount { get; init; }
    public bool DiscountApproved { get; init; }
    public string? DiscountApprovedBy { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
    public decimal Total { get; init; }
}

public record InvoiceResponse
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public Guid PatientId { get; init; }
    public Guid VisitId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string PatientUhid { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<InvoiceLineItemResponse> Items { get; init; } = [];
    public decimal GrossAmount { get; init; }
    public decimal TotalDiscount { get; init; }
    public decimal NetAmount { get; init; }

    /// <summary>Derived — Paid only once every line item is Paid, matching the frontend's
    /// getOverallPaymentStatus so the ledger and the detail page never disagree.</summary>
    public PaymentStatus PaymentStatus { get; init; }

    public bool IsVoided { get; init; }
    public DateTime? VoidedAt { get; init; }
    public string? VoidReason { get; init; }
}

/// <summary>Standard pagination/sort/search shape (docs/ApiStandards.md §6), plus the one
/// domain-specific filter the Ledger's toolbar offers.</summary>
public record InvoiceListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Sort { get; init; }
    public string? Search { get; init; }
    public PaymentStatus? PaymentStatus { get; init; }
}
