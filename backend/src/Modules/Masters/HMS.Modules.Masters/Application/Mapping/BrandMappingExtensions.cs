using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class BrandMappingExtensions
{
    public static BrandResponse ToResponse(this Brand brand) => new()
    {
        Id = brand.Id,
        BrandCode = brand.BrandCode,
        BrandName = brand.BrandName,
        Description = brand.Description,
        IsActive = brand.IsActive,
        CreatedAt = brand.CreatedAt,
        UpdatedAt = brand.UpdatedAt,
    };
}
