using HMS.Modules.HR.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Domain;

/// <summary>
/// An employee's request for time off — the aggregate root for the leave workflow. EmployeeId/
/// LeaveTypeId are real, same-schema FKs (both live in "hr" — see Attendance's remarks for why
/// this differs from Employee's cross-module Department/Designation references).
/// TotalDays is computed once, at creation, as an inclusive day count between StartDate and
/// EndDate — never trusted from the caller (see LeaveRequestService.CreateAsync). Approve/
/// Reject/Cancel are only valid from Pending; LeaveRequestService checks Status before calling
/// any of these (Domain trusts the caller already validated the transition, same convention as
/// every other cross-cutting business rule in this module).
/// </summary>
internal class LeaveRequest : Entity
{
    public Guid EmployeeId { get; private set; }
    public Guid LeaveTypeId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public int TotalDays { get; private set; }
    public string Reason { get; private set; } = null!;
    public LeaveRequestStatus Status { get; private set; } = LeaveRequestStatus.Pending;
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? DecisionNotes { get; private set; }

    // Required by EF Core materialization.
    private LeaveRequest()
    {
    }

    private LeaveRequest(
        Guid id,
        Guid employeeId,
        Guid leaveTypeId,
        DateOnly startDate,
        DateOnly endDate,
        int totalDays,
        string reason,
        Guid? createdBy)
        : base(id, createdBy)
    {
        EmployeeId = employeeId;
        LeaveTypeId = leaveTypeId;
        StartDate = startDate;
        EndDate = endDate;
        TotalDays = totalDays;
        Reason = reason;
        Status = LeaveRequestStatus.Pending;
    }

    public static LeaveRequest Create(
        Guid employeeId,
        Guid leaveTypeId,
        DateOnly startDate,
        DateOnly endDate,
        string reason,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(reason, nameof(reason));

        // Inclusive day count — a request from Monday to Monday is 1 day, not 0. Computed
        // here (not trusted from the caller) per the HR MVP spec.
        var totalDays = endDate.DayNumber - startDate.DayNumber + 1;

        // Time-ordered UUID per docs/DatabaseArchitecture.md §4 — same convention every
        // other aggregate root in this codebase uses.
        return new LeaveRequest(
            Guid.CreateVersion7(),
            employeeId,
            leaveTypeId,
            startDate,
            endDate,
            totalDays,
            reason.Trim(),
            createdBy);
    }

    /// <summary>Only valid from Pending — LeaveRequestService checks Status before calling
    /// this (see class remarks).</summary>
    public void Approve(Guid? actorUserId, string? notes)
    {
        Status = LeaveRequestStatus.Approved;
        ApprovedByUserId = actorUserId;
        ApprovedAt = DateTime.UtcNow;
        DecisionNotes = notes?.Trim();
        MarkUpdated(actorUserId);
    }

    /// <summary>Only valid from Pending. LeaveRequestService's request-contract validator
    /// requires a non-empty reason for a rejection; Domain itself doesn't re-enforce that
    /// (mirrors how every other request-shape rule in this codebase lives at the validator
    /// layer, not in Domain).</summary>
    public void Reject(Guid? actorUserId, string? notes)
    {
        Status = LeaveRequestStatus.Rejected;
        ApprovedByUserId = actorUserId;
        ApprovedAt = DateTime.UtcNow;
        DecisionNotes = notes?.Trim();
        MarkUpdated(actorUserId);
    }

    /// <summary>Only valid from Pending.</summary>
    public void Cancel(Guid? actorId)
    {
        Status = LeaveRequestStatus.Cancelled;
        MarkUpdated(actorId);
    }
}
