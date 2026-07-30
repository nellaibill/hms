using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreateBrandRequest
{
    public string BrandCode { get; init; } = string.Empty;
    public string BrandName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateBrandRequest
{
    public string BrandName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

public record BrandResponse
{
    public Guid Id { get; init; }
    public string BrandCode { get; init; } = string.Empty;
    public string BrandName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class BrandListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
