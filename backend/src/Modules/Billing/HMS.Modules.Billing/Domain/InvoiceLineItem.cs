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

    /// <summary>Same value as <see cref="ConsultantId"/> at creation, but never cleared by
    /// <see cref="MarkPaid"/> — a separate, reporting-only field so a Consultant Profit report
    /// can still attribute revenue after payment, without reversing ADR-048's deliberate "no
    /// longer currently assigned" behavior for <see cref="ConsultantId"/> itself.</summary>
    public string? BilledConsultantId { get; private set; }

    public string? ServiceId { get; private set; }

    /// <summary>App-level reference into Masters' DiagnosticPackage — set for a package line,
    /// null for a plain service line. Unlike DepartmentId/ConsultantId/ServiceId above this is
    /// a real Guid (Masters IDs are Guids, not the free-text billing-catalog identifiers those
    /// three carry), but the convention is otherwise identical: no DB FK, no existence
    /// validation at the Billing layer — Billing doesn't reach into Masters for this.</summary>
    public Guid? PackageId { get; private set; }

    /// <summary>The Radiology rate option billed (ADR-140) — an app-level reference into
    /// Masters' DiagnosticServiceRateOption, null for the service's standard rate. Reports read
    /// that option's doctor's share and running cost instead of the service's own.</summary>
    public Guid? RateOptionId { get; private set; }

    /// <summary>The option's name when billed (e.g. "LH"), kept so the bill still reads right if
    /// the option is renamed or removed later.</summary>
    public string? RateOptionName { get; private set; }

    /// <summary>True when the scan was done at the bedside (ADR-140): <see cref="UnitPrice"/>
    /// already includes the service's bedside add-on, and reports add its bedside doctor's
    /// share to the line's cost.</summary>
    public bool IsBedside { get; private set; }

    /// <summary>A bedside scan's add-on as charged (ADR-144), already inside
    /// <see cref="UnitPrice"/>. It starts at the service's bedside price but the counter may
    /// raise it. Null on every line that isn't a bedside scan.</summary>
    public decimal? BedsideAmount { get; private set; }

    /// <summary>The consultant's part of <see cref="BedsideAmount"/> (ADR-144): the amount less
    /// the hospital's bedside share in Masters, worked out when the line was billed. Null on a
    /// bedside line billed before ADR-144 — reports then use the service's bedside doctor's
    /// share from Masters, as before.</summary>
    public decimal? BedsideDoctorCharge { get; private set; }

    /// <summary>What one unit of this line cost, as Masters had it when it was billed (ADR-153):
    /// the consultant's share for a consultation; the doctor's share plus the running cost for a
    /// scan; the running cost for a lab test or package; the cost price for a procedure,
    /// injection or file. Kept on the line so a later Masters change can't rewrite the reports
    /// of days already billed. Null when Masters had no cost for it at billing time (and on
    /// lines billed before ADR-153 that the migration's fill couldn't cost): reports then read
    /// the current Masters, as before. Discounts are not taken off here; reports do that.</summary>
    public decimal? UnitCost { get; private set; }

    /// <summary>Radiology only (ADR-153): the doctor's share inside <see cref="UnitCost"/>, the
    /// bedside doctor's share included; the rest of <see cref="UnitCost"/> is the hospital's
    /// running cost. Null on every other line, and whenever <see cref="UnitCost"/> is null.</summary>
    public decimal? UnitDoctorShare { get; private set; }

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
        BilledConsultantId = ConsultantId;
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
        Guid? createdBy,
        Guid? rateOptionId = null,
        string? rateOptionName = null,
        bool isBedside = false,
        decimal? bedsideAmount = null,
        decimal? bedsideDoctorCharge = null,
        decimal? unitCost = null,
        decimal? unitDoctorShare = null)
    {
        var item = new InvoiceLineItem(
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
        item.RateOptionId = rateOptionId;
        item.RateOptionName = string.IsNullOrWhiteSpace(rateOptionName) ? null : rateOptionName.Trim();
        item.IsBedside = isBedside;
        item.BedsideAmount = isBedside ? bedsideAmount : null;
        item.BedsideDoctorCharge = isBedside ? bedsideDoctorCharge : null;
        item.UnitCost = unitCost;
        item.UnitDoctorShare = unitCost is null || billingType != BillingType.Radiology ? null : unitDoctorShare;
        return item;
    }

    /// <summary>Puts a Super Admin-approved discount on this line (ADR-109) and recomputes
    /// <see cref="Total"/> — the one exception to "computed once at creation", since a counter
    /// discount is decided after the bill is saved. Only an unpaid line can take one: collected
    /// money isn't re-priced. Callers (Invoice.ApplyDiscount) have already checked both rules so
    /// they can return a proper error; they are repeated here as invariants.</summary>
    public void ApplyDiscount(decimal discount, string? approvedByName, Guid? updatedBy)
    {
        if (PaymentStatus == PaymentStatus.Paid)
        {
            throw new InvalidOperationException("A paid line can't be discounted.");
        }
        if (discount < 0 || discount > Quantity * UnitPrice)
        {
            throw new ArgumentOutOfRangeException(nameof(discount), "A discount must be between zero and the line's charge.");
        }

        Discount = discount;
        DiscountApproved = true;
        DiscountApprovedBy = string.IsNullOrWhiteSpace(approvedByName) ? null : approvedByName.Trim();
        Total = Math.Max((Quantity * UnitPrice) - discount, 0m);
        MarkUpdated(updatedBy);
    }

    /// <summary>Caller (InvoiceService) is responsible for creating the corresponding
    /// Payment record — this only flips the denormalized status the ledger reads. Also
    /// clears ConsultantId: once a line item is paid, it's a settled transaction record, not
    /// an active assignment — a consultant shouldn't still read as "currently assigned" to a
    /// patient's paid bill (ADR-048 in docs/DecisionLog.md). BilledConsultantId deliberately
    /// does NOT get cleared here — see its own doc comment — so per-consultant reporting
    /// still works after payment even though the "currently assigned" field no longer does.</summary>
    public void MarkPaid(Guid? updatedBy)
    {
        PaymentStatus = PaymentStatus.Paid;
        ConsultantId = null;
        MarkUpdated(updatedBy);
    }
}
