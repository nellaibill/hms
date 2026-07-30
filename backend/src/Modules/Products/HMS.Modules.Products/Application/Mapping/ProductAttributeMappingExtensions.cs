using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;

namespace HMS.Modules.Products.Application.Mapping;

internal static class ProductAttributeMappingExtensions
{
    public static ProductAttributeResponse ToResponse(this ProductAttribute attribute) => new()
    {
        Id = attribute.Id,
        AttributeCode = attribute.AttributeCode,
        AttributeName = attribute.AttributeName,
        DataType = attribute.DataType,
        IsMandatory = attribute.IsMandatory,
        IsActive = attribute.IsActive,
        CreatedAt = attribute.CreatedAt,
        UpdatedAt = attribute.UpdatedAt,
    };
}
