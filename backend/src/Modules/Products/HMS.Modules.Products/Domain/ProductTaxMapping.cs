using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Domain;

/// <summary>Maps a product to an applicable tax (docs/04_Product_Management_ERD, business rule 6: "Tax can vary by tax type (e.g. GST, VAT)"). <see cref="TaxId"/> is a cross-schema reference into masters.taxes — see ProductTaxMappingConfiguration.</summary>
internal class ProductTaxMapping : Entity
{
    public Guid ProductId { get; private set; }
    public Guid TaxId { get; private set; }
    public string TaxType { get; private set; } = null!;
    public bool IsInclusive { get; private set; }
    public bool IsActive { get; private set; } = true;

    private ProductTaxMapping()
    {
    }

    private ProductTaxMapping(Guid id, Guid productId, Guid taxId, string taxType, bool isInclusive, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        ProductId = productId;
        TaxId = taxId;
        TaxType = taxType;
        IsInclusive = isInclusive;
        IsActive = isActive;
    }

    public static ProductTaxMapping Create(Guid productId, Guid taxId, string taxType, bool isInclusive, bool isActive, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(taxType, nameof(taxType));

        return new ProductTaxMapping(Guid.CreateVersion7(), productId, taxId, taxType.Trim(), isInclusive, isActive, createdBy);
    }

    public void Update(bool isInclusive, bool isActive, Guid? updatedBy)
    {
        IsInclusive = isInclusive;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
