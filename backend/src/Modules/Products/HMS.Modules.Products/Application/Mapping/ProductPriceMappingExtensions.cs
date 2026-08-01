using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;

namespace HMS.Modules.Products.Application.Mapping;

internal static class ProductPriceMappingExtensions
{
    public static ProductPriceResponse ToResponse(this ProductPrice price) => new()
    {
        Id = price.Id,
        ProductId = price.ProductId,
        PriceType = price.PriceType,
        CurrencyId = price.CurrencyId,
        Price = price.Price,
        EffectiveFrom = price.EffectiveFrom,
        EffectiveTo = price.EffectiveTo,
        IsActive = price.IsActive,
        CreatedAt = price.CreatedAt,
        UpdatedAt = price.UpdatedAt,
    };
}
