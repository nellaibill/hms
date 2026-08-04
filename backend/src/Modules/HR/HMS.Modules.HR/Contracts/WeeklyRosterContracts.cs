using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Contracts;

public record CreateWeeklyRosterRequest
{
    public DateOnly WeekStartDate { get; init; }
    public Guid DepartmentId { get; init; }
    public bool Published { get; init; }
    public DateTime? PublishedDate { get; init; }
}

// Every field is mutable via PUT — WeeklyRoster has no natural-key field to protect.
public record UpdateWeeklyRosterRequest
{
    public DateOnly WeekStartDate { get; init; }
    public Guid DepartmentId { get; init; }
    public bool Published { get; init; }
    public DateTime? PublishedDate { get; init; }
}

public record WeeklyRosterResponse
{
    public Guid Id { get; init; }
    public DateOnly WeekStartDate { get; init; }
    public Guid DepartmentId { get; init; }
    public bool Published { get; init; }
    public DateTime? PublishedDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class WeeklyRosterListQuery : PagedRequest
{
}

// POST /weekly-rosters/{id}/copy's body — the caller explicitly chooses the destination
// week rather than the endpoint silently adding 7 days to the source roster's week.
public record CopyWeeklyRosterRequest
{
    public DateOnly TargetWeekStartDate { get; init; }
}

// GET /weekly-rosters/monthly's query — a read-only view over the existing WeeklyRoster
// aggregate (rosters whose WeekStartDate falls in the given month), not a new entity.
// Mutable properties (not init), matching PagedRequest's own style — needed for
// [FromQuery] model binding.
public class MonthlyWeeklyRosterQuery : PagedRequest
{
    public int Year { get; set; }
    public int Month { get; set; }
}
