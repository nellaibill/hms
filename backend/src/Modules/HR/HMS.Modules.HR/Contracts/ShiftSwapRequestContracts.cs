using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Contracts;

// Named CreateSwapRequest/UpdateSwapRequest/SwapRequestResponse rather than the mechanical
// Create{Entity}Request template (which would stutter: CreateShiftSwapRequestRequest) —
// "SwapRequest" is the natural short form of ShiftSwapRequest and stays unambiguous within
// this module (no other "swap" concept exists here).
public record CreateSwapRequest
{
    public Guid RequestedByStaffId { get; init; }
    public Guid RequestedToStaffId { get; init; }
    public Guid CurrentShiftAssignmentId { get; init; }
    public Guid RequestedShiftAssignmentId { get; init; }

    // Nullable (unlike the non-nullable SwapRequestStatus on the entity itself): the
    // enum's default (ordinal 0 = Pending) is a legitimate real value, so "required" can
    // only be validated if a missing value is representable at all — same treatment as
    // AvailabilityStatus in Phase 4.
    public SwapRequestStatus? Status { get; init; }

    public DateTime RequestedDate { get; init; }
    public DateTime? ApprovedDate { get; init; }
    public Guid? ApprovedBy { get; init; }
    public string? Remarks { get; init; }
}

// Every field is mutable via PUT — ShiftSwapRequest has no natural-key field to protect.
public record UpdateSwapRequest
{
    public Guid RequestedByStaffId { get; init; }
    public Guid RequestedToStaffId { get; init; }
    public Guid CurrentShiftAssignmentId { get; init; }
    public Guid RequestedShiftAssignmentId { get; init; }
    public SwapRequestStatus? Status { get; init; }
    public DateTime RequestedDate { get; init; }
    public DateTime? ApprovedDate { get; init; }
    public Guid? ApprovedBy { get; init; }
    public string? Remarks { get; init; }
}

public record SwapRequestResponse
{
    public Guid Id { get; init; }
    public Guid RequestedByStaffId { get; init; }
    public Guid RequestedToStaffId { get; init; }
    public Guid CurrentShiftAssignmentId { get; init; }
    public Guid RequestedShiftAssignmentId { get; init; }
    public SwapRequestStatus Status { get; init; }
    public DateTime RequestedDate { get; init; }
    public DateTime? ApprovedDate { get; init; }
    public Guid? ApprovedBy { get; init; }
    public string? Remarks { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class SwapRequestListQuery : PagedRequest
{
}
