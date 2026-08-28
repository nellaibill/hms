using HMS.Modules.Billing.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Billing.Domain;

/// <summary>
/// One priced line on an <see cref="Invoice"/> — mirrors frontend/web/src/features/billing
/// /types.ts's BillingItem. <see cref="BillingType"/> is the category discriminator (a
/// unified shape covers Consultation/Radiology/Laboratory/Procedure, not four separate
/// tables — see docs/DecisionLog.md).
/// </summary>
internal class InvoiceLineItem : Entity
{
    public Guid InvoiceId { get; private set; }
    public BillingType BillingType { get; private set; }

    /// <summary>Free-text identifiers into the frontend's own billing reference catalog
    /// (frontend/web/src/features/billing/billingCatalog.ts — department/consultant/service
    /// names for consultation and per-category service pricing), not Masters-backed Guid
    /// FKs. Billing pricing/staff catalogs are a separate concern from Masters' hospital-wide
    /// department/consultant directory (used by Patient registration and IPD) and aren't
    /// unified with it in this iteration — see docs/DecisionLog.md's Billing ADR.</summary>
    public string? DepartmentId { get; private set; }
    public string? ConsultantId { get; private set; }
    public string? ServiceId { get; private set; }

    /// <summary>App-level reference into Masters' DiagnosticPackage — set for a package line,
    /// null for a plain service line. Unlike DepartmentId/ConsultantId/ServiceId above this is
    /// a real Guid (Masters IDs are Guids, not the free-text billing-catalog identifiers those
    /// three carry), but the convention is otherwise identical: no DB FK, no existence
    /// validation at the Billing layer — Billing doesn't reach into Masters for this.</summary>
    public Guid? PackageId { get; private set; }

    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Discount { get; private set; }
    public bool DiscountApproved { get; private set; }
    public string? DiscountApprovedBy { get; private set; }

    /// <summary>Denormalized for fast ledger filtering — flipped to Paid by
    /// <see cref="MarkPaid"/> when InvoiceService records a Payment against this line.</summary>
    public PaymentStatus PaymentStatus { get; private set; }

    /// <summary>(Quantity × UnitPrice) − Discount, clamped at 0 — computed once at creation,
    /// not recomputed on read, so a historical invoice's total can't drift if unit-price
    /// catalogs change later.</summary>
    public decimal Total { get; private set; }

    // Required by EF Core materialization.
    private InvoiceLineItem()
    {
    }

    private InvoiceLineItem(
        Guid id,
        Guid invoiceId,
        BillingType billingType,
        string? departmentId,
        string? consultantId,
        string? serviceId,
        Guid? packageId,
        int quantity,
        decimal unitPrice,
        decimal discount,
        bool discountApproved,
        string? discountApprovedBy,
        Guid? createdBy)
        : base(id, createdBy)
    {
        InvoiceId = invoiceId;
        BillingType = billingType;
        DepartmentId = string.IsNullOrWhiteSpace(departmentId) ? null : departmentId.Trim();
        ConsultantId = string.IsNullOrWhiteSpace(consultantId) ? null : consultantId.Trim();
        ServiceId = serviceId;
        PackageId = packageId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Discount = discount;
        DiscountApproved = discountApproved;
        DiscountApprovedBy = discountApprovedBy;
        PaymentStatus = PaymentStatus.Pending;
        Total = Math.Max((quantity * unitPrice) - discount, 0m);
    }

    public static InvoiceLineItem Create(
        Guid invoiceId,
        BillingType billingType,
        string? departmentId,
        string? consultantId,
        string? serviceId,
        Guid? packageId,
        int quantity,
        decimal unitPrice,
        decimal discount,
        bool discountApproved,
        string? discountApprovedBy,
        Guid? createdBy)
    {
        return new InvoiceLineItem(
            Guid.CreateVersion7(),
            invoiceId,
            billingType,
            departmentId,
            consultantId,
            string.IsNullOrWhiteSpace(serviceId) ? null : serviceId.Trim(),
            packageId,
            quantity,
            unitPrice,
            discount,
            discountApproved,
            string.IsNullOrWhiteSpace(discountApprovedBy) ? null : discountApprovedBy.Trim(),
            createdBy);
    }

    /// <summary>Caller (InvoiceService) is responsible for creating the corresponding
    /// Payment record — this only flips the denormalized status the ledger reads.</summary>
    public void MarkPaid(Guid? updatedBy)
    {
        PaymentStatus = PaymentStatus.Paid;
        MarkUpdated(updatedBy);
    }
}
