using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Contracts;

public record CreateStaffAvailabilityRequest
{
    public Guid StaffId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }

    // Nullable (unlike the non-nullable AvailabilityStatus on the entity itself):
    // AvailabilityStatus's default (ordinal 0 = Available) is a fully legitimate real
    // value, so "required" can only be validated if a missing value is representable at
    // all, which means nullable in the wire contract — same reasoning as Shift's
    // StartTime/EndTime in Phase 1.
    public AvailabilityStatus? AvailabilityStatus { get; init; }

    public string? Reason { get; init; }
}

// Every field is mutable via PUT — StaffAvailability has no natural-key field to protect.
public record UpdateStaffAvailabilityRequest
{
    public Guid StaffId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public AvailabilityStatus? AvailabilityStatus { get; init; }
    public string? Reason { get; init; }
}

public record StaffAvailabilityResponse
{
    public Guid Id { get; init; }
    public Guid StaffId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public AvailabilityStatus AvailabilityStatus { get; init; }
    public string? Reason { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class StaffAvailabilityListQuery : PagedRequest
{
}
