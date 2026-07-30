using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreateProductCategoryRequest
{
    public string CategoryCode { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public Guid? ParentId { get; init; }
    public int SortOrder { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateProductCategoryRequest
{
    public string CategoryName { get; init; } = string.Empty;
    public Guid? ParentId { get; init; }
    public int SortOrder { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

public record ProductCategoryResponse
{
    public Guid Id { get; init; }
    public string CategoryCode { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public Guid? ParentId { get; init; }
    public int SortOrder { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class ProductCategoryListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
