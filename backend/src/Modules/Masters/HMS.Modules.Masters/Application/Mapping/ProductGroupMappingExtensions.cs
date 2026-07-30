using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class ProductGroupMappingExtensions
{
    public static ProductGroupResponse ToResponse(this ProductGroup group) => new()
    {
        Id = group.Id,
        GroupCode = group.GroupCode,
        GroupName = group.GroupName,
        SubCategoryId = group.SubCategoryId,
        SortOrder = group.SortOrder,
        Description = group.Description,
        IsActive = group.IsActive,
        CreatedAt = group.CreatedAt,
        UpdatedAt = group.UpdatedAt,
    };
}
