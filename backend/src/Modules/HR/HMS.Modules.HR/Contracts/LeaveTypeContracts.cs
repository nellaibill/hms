using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Contracts;

public record CreateLeaveTypeRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    /// <summary>Null means unlimited.</summary>
    public int? MaxDaysPerYear { get; init; }
    public bool IsPaid { get; init; }
    public bool IsActive { get; init; } = true;
}

// Code is intentionally absent — a natural-key field, protected from change after creation.
public record UpdateLeaveTypeRequest
{
    public string Name { get; init; } = string.Empty;
    public int? MaxDaysPerYear { get; init; }
    public bool IsPaid { get; init; }
    public bool IsActive { get; init; } = true;
}

public record LeaveTypeResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int? MaxDaysPerYear { get; init; }
    public bool IsPaid { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class LeaveTypeListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
