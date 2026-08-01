using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class ProductCategoryMappingExtensions
{
    public static ProductCategoryResponse ToResponse(this ProductCategory category) => new()
    {
        Id = category.Id,
        CategoryCode = category.CategoryCode,
        CategoryName = category.CategoryName,
        ParentId = category.ParentId,
        SortOrder = category.SortOrder,
        Description = category.Description,
        IsActive = category.IsActive,
        CreatedAt = category.CreatedAt,
        UpdatedAt = category.UpdatedAt,
    };
}
