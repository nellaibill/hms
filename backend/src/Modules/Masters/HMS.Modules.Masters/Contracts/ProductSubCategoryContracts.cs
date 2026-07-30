using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreateProductSubCategoryRequest
{
    public string SubCategoryCode { get; init; } = string.Empty;
    public string SubCategoryName { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public int SortOrder { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateProductSubCategoryRequest
{
    public string SubCategoryName { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public int SortOrder { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

public record ProductSubCategoryResponse
{
    public Guid Id { get; init; }
    public string SubCategoryCode { get; init; } = string.Empty;
    public string SubCategoryName { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public int SortOrder { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class ProductSubCategoryListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
    public Guid? CategoryId { get; set; }
}
