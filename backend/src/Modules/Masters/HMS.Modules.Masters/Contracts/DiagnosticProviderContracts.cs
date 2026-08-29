using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreateDiagnosticProviderRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ContactDetails { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateDiagnosticProviderRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ContactDetails { get; init; }
    public bool IsActive { get; init; } = true;
}

public record DiagnosticProviderResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ContactDetails { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class DiagnosticProviderListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
