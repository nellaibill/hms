using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreateDepartmentRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
}

// Code is intentionally absent — a natural-key field, protected from change after creation.
public record UpdateDepartmentRequest
{
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
}

public record DepartmentResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class DepartmentListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
