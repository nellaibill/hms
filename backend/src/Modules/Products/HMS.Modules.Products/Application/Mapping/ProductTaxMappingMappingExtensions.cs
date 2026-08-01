using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;

namespace HMS.Modules.Products.Application.Mapping;

internal static class ProductTaxMappingMappingExtensions
{
    public static ProductTaxMappingResponse ToResponse(this ProductTaxMapping mapping) => new()
    {
        Id = mapping.Id,
        ProductId = mapping.ProductId,
        TaxId = mapping.TaxId,
        TaxType = mapping.TaxType,
        IsInclusive = mapping.IsInclusive,
        IsActive = mapping.IsActive,
        CreatedAt = mapping.CreatedAt,
        UpdatedAt = mapping.UpdatedAt,
    };
}
