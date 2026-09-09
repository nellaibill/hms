namespace HMS.Modules.IPD.Contracts;

public enum WardType
{
    General,
    SemiPrivate,
    Private,
    ICU,
}

public enum BedStatus
{
    Available,
    Occupied,
    Maintenance,
}

public enum BedType
{
    Standard,
    Electric,
    ICU,
    SemiICU,
    Deluxe,
}

public enum AdmissionType
{
    Emergency,
    Elective,
    Transfer,
}

public enum AdmissionStatus
{
    Admitted,
    Discharged,
}

public enum DischargeType
{
    Normal,
    AgainstMedicalAdvice,
    Referred,
}

public enum ChargeType
{
    AdmissionCharge,
    BedCharge,
    NursingCharge,

    /// <summary>Auto-posted by IPDLabOrderService when a ward doctor places a lab order
    /// directly on an admission (see Application/IPDLabOrderService.cs) — the price is
    /// resolved from Masters at order-placement time; Remarks carries the test/package name
    /// since AdmissionCharge has no FK back to the LabOrder/LabOrderItem that generated it.</summary>
    LabCharge,

    /// <summary>Auto-posted by DoctorOrderService.CreateAsync when a DoctorOrder is placed
    /// against a priced Masters catalog item (Radiology/Procedure/Consultation only — see
    /// DoctorOrder.CatalogItemId) — one generic type covers all three rather than one enum
    /// value per OrderType, Remarks distinguishes which (e.g. "Radiology: Chest X-ray").
    /// See ADR-065.</summary>
    DoctorOrderCharge,
}

/// <summary>Named `NursingShift` (not `Shift`) to avoid any confusion with HR's unrelated
/// `Shift`/`ShiftAssignment` entities — different module, different concept.</summary>
public enum NursingShift
{
    Morning,
    Evening,
    Night,
}

/// <summary>
/// Deliberately excludes Laboratory (HMS.Modules.Laboratory already has a full LabOrder
/// workflow — a generic entry here would be a disconnected duplicate) and Medication (belongs
/// to the not-yet-built MAR slice, which needs a real structured prescription entity, not a
/// free-text order description here that MAR would immediately have to replace). See
/// docs/DecisionLog.md.
/// </summary>
public enum DoctorOrderType
{
    Radiology,
    Procedure,
    Diet,
    Nursing,
    Blood,
    Consultation,
    Referral,
}

/// <summary>Fixed linear sequence Ordered -> Accepted -> InProgress -> Completed, with
/// Cancelled reachable from any non-terminal state. See Domain/DoctorOrder.cs's
/// Advance/Cancel methods for the exact legal transitions.</summary>
public enum DoctorOrderStatus
{
    Ordered,
    Accepted,
    InProgress,
    Completed,
    Cancelled,
}

/// <summary>Only two states — a one-way Active -> Discontinued transition. "Past its end
/// date" is a displayed, not stored, fact the frontend derives by comparing EndDate to now;
/// there's no auto-transition to a "Completed" status, avoiding a background job. See
/// Domain/MedicationOrder.cs's Discontinue method.</summary>
public enum MedicationOrderStatus
{
    Active,
    Discontinued,
}

/// <summary>How an AdmissionAdvance was collected — a small local mirror of
/// HMS.Modules.Billing.Contracts.PaymentMethod's four values (kept as IPD's own type rather
/// than a cross-module reference in IPD's public contracts, same as every other small fixed
/// vocabulary in this module), not admin-editable reference data. See ADR-067.</summary>
public enum PaymentMethod
{
    Cash,
    Card,
    Upi,
    BankTransfer,
}
