using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Contracts;

public record CreateWardRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public Guid DepartmentId { get; init; }
    public WardType WardType { get; init; }
    public bool IsActive { get; init; } = true;
}

// Code is intentionally absent — a natural-key field, protected from change after creation.
public record UpdateWardRequest
{
    public string Name { get; init; } = string.Empty;
    public Guid DepartmentId { get; init; }
    public WardType WardType { get; init; }
    public bool IsActive { get; init; } = true;
}

public record WardResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public Guid DepartmentId { get; init; }
    public WardType WardType { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class WardListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
    public Guid? DepartmentId { get; set; }
}
