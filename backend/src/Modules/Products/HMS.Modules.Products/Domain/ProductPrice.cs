using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Domain;

/// <summary>Time-bounded price entry for a product (docs/04_Product_Management_ERD, business rule 4: "Price is maintained with validity period"). <see cref="CurrencyId"/> is a cross-schema reference into masters.currencies — see ProductPriceConfiguration.</summary>
internal class ProductPrice : Entity
{
    public Guid ProductId { get; private set; }
    public string PriceType { get; private set; } = null!;
    public Guid CurrencyId { get; private set; }
    public decimal Price { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; } = true;

    private ProductPrice()
    {
    }

    private ProductPrice(Guid id, Guid productId, string priceType, Guid currencyId, decimal price, DateOnly effectiveFrom, DateOnly? effectiveTo, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        ProductId = productId;
        PriceType = priceType;
        CurrencyId = currencyId;
        Price = price;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        IsActive = isActive;
    }

    public static ProductPrice Create(Guid productId, string priceType, Guid currencyId, decimal price, DateOnly effectiveFrom, DateOnly? effectiveTo, bool isActive, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(priceType, nameof(priceType));

        return new ProductPrice(Guid.CreateVersion7(), productId, priceType.Trim(), currencyId, price, effectiveFrom, effectiveTo, isActive, createdBy);
    }

    public void Update(decimal price, DateOnly effectiveFrom, DateOnly? effectiveTo, bool isActive, Guid? updatedBy)
    {
        Price = price;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
