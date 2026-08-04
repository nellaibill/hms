using HMS.Modules.HR.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Domain;

/// <summary>
/// Records a request from one staff member to exchange a shift with another — the
/// aggregate root for Phase 5. CRUD only: no approval workflow, no notification, and no
/// automatic assignment changes happen as a side effect of any state here — Status is
/// simply stored, per the Phase 5 spec. CurrentShiftAssignmentId/RequestedShiftAssignmentId
/// are validated for existence by ShiftSwapRequestService (application-layer referential
/// validation only), not enforced as a database foreign key — no new FK relationship was
/// requested for this phase.
/// </summary>
internal class ShiftSwapRequest : Entity
{
    public Guid RequestedByStaffId { get; private set; }
    public Guid RequestedToStaffId { get; private set; }
    public Guid CurrentShiftAssignmentId { get; private set; }
    public Guid RequestedShiftAssignmentId { get; private set; }
    public SwapRequestStatus Status { get; private set; }
    public DateTime RequestedDate { get; private set; }
    public DateTime? ApprovedDate { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public string? Remarks { get; private set; }

    // Required by EF Core materialization.
    private ShiftSwapRequest()
    {
    }

    private ShiftSwapRequest(
        Guid id,
        Guid requestedByStaffId,
        Guid requestedToStaffId,
        Guid currentShiftAssignmentId,
        Guid requestedShiftAssignmentId,
        SwapRequestStatus status,
        DateTime requestedDate,
        DateTime? approvedDate,
        Guid? approvedBy,
        string? remarks,
        Guid? createdBy)
        : base(id, createdBy)
    {
        RequestedByStaffId = requestedByStaffId;
        RequestedToStaffId = requestedToStaffId;
        CurrentShiftAssignmentId = currentShiftAssignmentId;
        RequestedShiftAssignmentId = requestedShiftAssignmentId;
        Status = status;
        RequestedDate = requestedDate;
        ApprovedDate = approvedDate;
        ApprovedBy = approvedBy;
        Remarks = remarks;
    }

    // Deliberately no guard clauses against Guid.Empty/default(DateTime) — required-ness
    // for these fields is enforced at the validator layer only, matching every other
    // aggregate in this module. No rule against RequestedByStaffId == RequestedToStaffId,
    // no ApprovedBy/ApprovedDate consistency check — all explicitly out of scope.
    public static ShiftSwapRequest Create(
        Guid requestedByStaffId,
        Guid requestedToStaffId,
        Guid currentShiftAssignmentId,
        Guid requestedShiftAssignmentId,
        SwapRequestStatus status,
        DateTime requestedDate,
        DateTime? approvedDate,
        Guid? approvedBy,
        string? remarks,
        Guid? createdBy)
    {
        // Time-ordered UUID per docs/DatabaseArchitecture.md §4 — same convention every
        // other aggregate root in this codebase uses.
        return new ShiftSwapRequest(
            Guid.CreateVersion7(),
            requestedByStaffId,
            requestedToStaffId,
            currentShiftAssignmentId,
            requestedShiftAssignmentId,
            status,
            requestedDate,
            approvedDate,
            approvedBy,
            remarks?.Trim(),
            createdBy);
    }

    public void Update(
        Guid requestedByStaffId,
        Guid requestedToStaffId,
        Guid currentShiftAssignmentId,
        Guid requestedShiftAssignmentId,
        SwapRequestStatus status,
        DateTime requestedDate,
        DateTime? approvedDate,
        Guid? approvedBy,
        string? remarks,
        Guid? updatedBy)
    {
        RequestedByStaffId = requestedByStaffId;
        RequestedToStaffId = requestedToStaffId;
        CurrentShiftAssignmentId = currentShiftAssignmentId;
        RequestedShiftAssignmentId = requestedShiftAssignmentId;
        Status = status;
        RequestedDate = requestedDate;
        ApprovedDate = approvedDate;
        ApprovedBy = approvedBy;
        Remarks = remarks?.Trim();
        MarkUpdated(updatedBy);
    }
}
