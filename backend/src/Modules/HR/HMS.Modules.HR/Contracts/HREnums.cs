namespace HMS.Modules.HR.Contracts;

/// <summary>
/// Shared vocabulary for the HR module — public because these values cross the HTTP
/// boundary (request/response fields) and Swagger needs to describe them, but also used
/// directly by Domain/Application within this same assembly (mirrors
/// HMS.Modules.Patients.Contracts.PatientEnums — see docs/DecisionLog.md for why enums are
/// the one type Domain is allowed to share with Contracts).
/// </summary>
public enum AssignmentStatus
{
    Scheduled,
    Completed,
    Cancelled,
}

/// <summary>
/// Just the binary availability state — "Available" or "Unavailable". Anything more
/// specific (Conference, Training, Medical Leave, ...) is free text in
/// StaffAvailability.Reason, not a status value.
/// </summary>
public enum AvailabilityStatus
{
    Available,
    Unavailable,
}

/// <summary>
/// The lifecycle state of a shift swap request. No logic is attached to any of these
/// values — nothing here enforces valid transitions or reacts to a status change; per the
/// Phase 5 spec, approval workflow is explicitly out of scope and Status is simply stored.
/// </summary>
public enum SwapRequestStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
}
