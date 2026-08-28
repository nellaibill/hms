using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreateDiagnosticPackageRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    /// <summary>A deliberate bundle-discount price — independent of the sum of the package's
    /// item prices, never auto-computed. See Domain/DiagnosticPackage.cs.</summary>
    public decimal TotalPrice { get; init; }
    public bool IsActive { get; init; } = true;
    /// <summary>At least one DiagnosticService id is required — a package with zero tests
    /// isn't a bundle worth creating.</summary>
    public IReadOnlyList<Guid> ServiceIds { get; init; } = [];
}

public record UpdateDiagnosticPackageRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal TotalPrice { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>Adds one test to an existing package ("Add another Test" on the package detail
/// page) — mirrors AddAllergyRequest's one-row-at-a-time shape.</summary>
public record AddDiagnosticPackageItemRequest
{
    public Guid ServiceId { get; init; }
}

public record DiagnosticPackageItemResponse
{
    public Guid Id { get; init; }
    public Guid ServiceId { get; init; }
}

public record DiagnosticPackageResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal TotalPrice { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyList<DiagnosticPackageItemResponse> Items { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class DiagnosticPackageListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
