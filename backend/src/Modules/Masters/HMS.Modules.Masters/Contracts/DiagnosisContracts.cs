using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreateDiagnosisRequest
{
    public string Name { get; init; } = string.Empty;
    public string? IcdCode { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateDiagnosisRequest
{
    public string Name { get; init; } = string.Empty;
    public string? IcdCode { get; init; }
    public bool IsActive { get; init; } = true;
}

public record DiagnosisResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? IcdCode { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class DiagnosisListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
