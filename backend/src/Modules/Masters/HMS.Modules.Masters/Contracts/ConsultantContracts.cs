using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreateConsultantRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public Guid? DepartmentId { get; init; }
    public string? Specialization { get; init; }
    public bool IsActive { get; init; } = true;
}

// Code is intentionally absent — a natural-key field, protected from change after creation.
public record UpdateConsultantRequest
{
    public string Name { get; init; } = string.Empty;
    public Guid? DepartmentId { get; init; }
    public string? Specialization { get; init; }
    public bool IsActive { get; init; } = true;
}

public record ConsultantResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public Guid? DepartmentId { get; init; }
    public string? Specialization { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class ConsultantListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
    public Guid? DepartmentId { get; set; }
}
