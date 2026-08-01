using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Contracts;

public record CreateProductPriceRequest
{
    public string PriceType { get; init; } = string.Empty;
    public Guid CurrencyId { get; init; }
    public decimal Price { get; init; }
    public DateOnly EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateProductPriceRequest
{
    public decimal Price { get; init; }
    public DateOnly EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; init; }
    public bool IsActive { get; init; } = true;
}

public record ProductPriceResponse
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string PriceType { get; init; } = string.Empty;
    public Guid CurrencyId { get; init; }
    public decimal Price { get; init; }
    public DateOnly EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class ProductPriceListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
    public string? PriceType { get; set; }
}
