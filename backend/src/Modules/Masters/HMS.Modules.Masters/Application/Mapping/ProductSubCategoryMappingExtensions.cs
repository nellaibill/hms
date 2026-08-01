using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class ProductSubCategoryMappingExtensions
{
    public static ProductSubCategoryResponse ToResponse(this ProductSubCategory subCategory) => new()
    {
        Id = subCategory.Id,
        SubCategoryCode = subCategory.SubCategoryCode,
        SubCategoryName = subCategory.SubCategoryName,
        CategoryId = subCategory.CategoryId,
        SortOrder = subCategory.SortOrder,
        Description = subCategory.Description,
        IsActive = subCategory.IsActive,
        CreatedAt = subCategory.CreatedAt,
        UpdatedAt = subCategory.UpdatedAt,
    };
}
