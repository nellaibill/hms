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
