namespace HMS.Modules.Billing.Contracts;

/// <summary>
/// Mirrors frontend/web/src/features/billing/types.ts's BILLING_TYPES — a category on a
/// line item, not a separate table per type. One Invoice/InvoiceLineItem shape covers every
/// category (see docs/DecisionLog.md's unified-invoice-engine ADR), matching the "single
/// billing/invoice engine" recommendation in docs/BusinessRequirementsAnalysis.md instead of
/// the four near-duplicate blocks the client's original brief described.
/// </summary>
public enum BillingType
{
    Consultation,
    Radiology,
    Laboratory,
    Procedure,

    /// <summary>Injections administered during a visit — no consultant/doctor involved, just a
    /// priced service (see Masters' DiagnosticTestServiceType.Injection).</summary>
    Injection,

    /// <summary>File/documentation charges (General Blue File, ANC File…) — no consultant/
    /// doctor involved, just a priced service (see Masters' DiagnosticTestServiceType.File).</summary>
    File,

    /// <summary>A Pharmacy dispense (HMS.Modules.Pharmacy.Application.DispenseService) — the
    /// one line item's UnitPrice is the dispense's total price (Quantity × Product.SellingPrice)
    /// rather than a per-unit rate, because this record's own Quantity is `int` (see this
    /// enum's own doc comment on the unified line-item shape) and a dispense's real quantity
    /// is decimal (e.g. 150.5ml of a syrup) — see docs/DecisionLog.md ADR-028.</summary>
    Pharmacy,
}

/// <summary>Per-line-item status — an invoice's own overall status is derived (every item
/// Paid) rather than stored, matching the frontend's getOverallPaymentStatus.</summary>
public enum PaymentStatus
{
    Pending,
    Paid,
}

/// <summary>How a payment was collected — not tied to Masters' PaymentMethod master data
/// (that catalog is for Supplier/Customer payment terms, a different concern); kept as a
/// small fixed enum here since front-desk collection methods don't need to be admin-editable
/// reference data the way tax rates or currencies do.</summary>
public enum PaymentMethod
{
    Cash,
    Card,
    Upi,
    BankTransfer,
}
