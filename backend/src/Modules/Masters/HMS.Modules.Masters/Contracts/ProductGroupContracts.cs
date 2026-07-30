using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreateProductGroupRequest
{
    public string GroupCode { get; init; } = string.Empty;
    public string GroupName { get; init; } = string.Empty;
    public Guid SubCategoryId { get; init; }
    public int SortOrder { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateProductGroupRequest
{
    public string GroupName { get; init; } = string.Empty;
    public Guid SubCategoryId { get; init; }
    public int SortOrder { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

public record ProductGroupResponse
{
    public Guid Id { get; init; }
    public string GroupCode { get; init; } = string.Empty;
    public string GroupName { get; init; } = string.Empty;
    public Guid SubCategoryId { get; init; }
    public int SortOrder { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class ProductGroupListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
    public Guid? SubCategoryId { get; set; }
}
