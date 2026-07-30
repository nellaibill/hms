using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;

namespace HMS.Modules.Products.Application.Mapping;

internal static class ProductImageMappingExtensions
{
    public static ProductImageResponse ToResponse(this ProductImage image) => new()
    {
        Id = image.Id,
        ProductId = image.ProductId,
        ImageUrl = image.ImageUrl,
        ImageType = image.ImageType,
        IsPrimary = image.IsPrimary,
        DisplayOrder = image.DisplayOrder,
        IsActive = image.IsActive,
        CreatedAt = image.CreatedAt,
        UpdatedAt = image.UpdatedAt,
    };
}
