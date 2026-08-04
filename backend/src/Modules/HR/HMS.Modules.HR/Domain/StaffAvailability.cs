using HMS.Modules.HR.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Domain;

/// <summary>
/// Records whether a staff member is available or unavailable during a date range — the
/// aggregate root for Phase 4. Purely informational: nothing yet reads this to block or
/// validate ShiftAssignments (see docs/DecisionLog.md — that belongs to a future phase).
/// AvailabilityStatus is just the binary Available/Unavailable state; anything more
/// specific (Conference, Training, Medical Leave, ...) belongs in Reason, not the status.
/// </summary>
internal class StaffAvailability : Entity
{
    public Guid StaffId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public AvailabilityStatus AvailabilityStatus { get; private set; }
    public string? Reason { get; private set; }

    // Required by EF Core materialization.
    private StaffAvailability()
    {
    }

    private StaffAvailability(
        Guid id,
        Guid staffId,
        DateOnly startDate,
        DateOnly endDate,
        AvailabilityStatus availabilityStatus,
        string? reason,
        Guid? createdBy)
        : base(id, createdBy)
    {
        StaffId = staffId;
        StartDate = startDate;
        EndDate = endDate;
        AvailabilityStatus = availabilityStatus;
        Reason = reason;
    }

    // Deliberately no guard clause against Guid.Empty for staffId, and no EndDate >=
    // StartDate check — required-ness for Guid/date fields is enforced at the validator
    // layer only (matches ShiftAssignment), and date-order/overlap rules are explicitly
    // out of scope for this phase.
    public static StaffAvailability Create(
        Guid staffId,
        DateOnly startDate,
        DateOnly endDate,
        AvailabilityStatus availabilityStatus,
        string? reason,
        Guid? createdBy)
    {
        // Time-ordered UUID per docs/DatabaseArchitecture.md §4 — same convention every
        // other aggregate root in this codebase uses.
        return new StaffAvailability(Guid.CreateVersion7(), staffId, startDate, endDate, availabilityStatus, reason?.Trim(), createdBy);
    }

    public void Update(
        Guid staffId,
        DateOnly startDate,
        DateOnly endDate,
        AvailabilityStatus availabilityStatus,
        string? reason,
        Guid? updatedBy)
    {
        StaffId = staffId;
        StartDate = startDate;
        EndDate = endDate;
        AvailabilityStatus = availabilityStatus;
        Reason = reason?.Trim();
        MarkUpdated(updatedBy);
    }
}
