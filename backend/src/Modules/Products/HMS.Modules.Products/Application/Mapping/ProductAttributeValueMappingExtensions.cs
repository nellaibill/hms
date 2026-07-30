using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;

namespace HMS.Modules.Products.Application.Mapping;

internal static class ProductAttributeValueMappingExtensions
{
    public static ProductAttributeValueResponse ToResponse(this ProductAttributeValue value) => new()
    {
        Id = value.Id,
        ProductId = value.ProductId,
        AttributeId = value.AttributeId,
        AttributeValue = value.AttributeValue,
        IsActive = value.IsActive,
        CreatedAt = value.CreatedAt,
        UpdatedAt = value.UpdatedAt,
    };
}
