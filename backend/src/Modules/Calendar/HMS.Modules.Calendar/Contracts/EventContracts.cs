using HMS.Shared.Kernel;

namespace HMS.Modules.Calendar.Contracts;

public record CreateEventRequest
{
    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    // Nullable here (unlike the non-nullable EventType on Event itself): the enum's
    // default (ordinal 0 = Holiday) is a legitimate real value, so "required" can only
    // be validated if a missing value is representable at all — same treatment as
    // AvailabilityStatus/SwapRequestStatus in the HR module.
    public EventType? EventType { get; init; }

    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }

    public bool IsAllDay { get; init; }

    public Guid? DepartmentId { get; init; }
}

public record UpdateEventRequest
{
    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public EventType? EventType { get; init; }

    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }

    public bool IsAllDay { get; init; }

    public Guid? DepartmentId { get; init; }
}

public record EventResponse
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public EventType EventType { get; init; }

    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }

    public bool IsAllDay { get; init; }

    public Guid? DepartmentId { get; init; }

    public Guid? CreatedBy { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

public class EventListQuery : PagedRequest
{
    public EventType? EventType { get; set; }

    public Guid? DepartmentId { get; set; }
}

/// <summary>GET /events/month's query — a read-only view over the existing Event
/// aggregate (events whose date range intersects the given month), not a new entity.
/// Mirrors HR's MonthlyWeeklyRosterQuery.</summary>
public class MonthlyEventQuery : PagedRequest
{
    public int Year { get; set; }

    public int Month { get; set; }

    public EventType? EventType { get; set; }

    public Guid? DepartmentId { get; set; }
}

/// <summary>
/// POST /events/bulk's body — for loading a batch of events in one request (e.g. a
/// year's national holiday list) instead of one HTTP call per event. Not a new entity
/// or a new business rule: each item runs through the exact same validation and
/// creation logic POST /events already applies, one at a time, so a bad item in the
/// batch doesn't need to invalidate the whole request.
/// </summary>
public record BulkCreateEventsRequest
{
    public IReadOnlyList<CreateEventRequest> Events { get; init; } = [];
}

/// <summary>One item's outcome within a bulk create — Index matches its position in
/// the original request so the caller can tell which input row a failure belongs to.</summary>
public record BulkCreateEventResult
{
    public int Index { get; init; }

    public bool Success { get; init; }

    public EventResponse? Event { get; init; }

    public string? ErrorCode { get; init; }

    public string? Error { get; init; }
}

public record BulkCreateEventsResponse
{
    public IReadOnlyList<BulkCreateEventResult> Results { get; init; } = [];

    public int SucceededCount { get; init; }

    public int FailedCount { get; init; }
}
