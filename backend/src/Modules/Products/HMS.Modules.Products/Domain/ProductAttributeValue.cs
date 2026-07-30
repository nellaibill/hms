using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Domain;

/// <summary>The EAV value row linking a <see cref="Domain.Product"/> to a <see cref="ProductAttribute"/> definition and its value for that product.</summary>
internal class ProductAttributeValue : Entity
{
    public Guid ProductId { get; private set; }
    public Guid AttributeId { get; private set; }
    public string AttributeValue { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;

    private ProductAttributeValue()
    {
    }

    private ProductAttributeValue(Guid id, Guid productId, Guid attributeId, string attributeValue, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        ProductId = productId;
        AttributeId = attributeId;
        AttributeValue = attributeValue;
        IsActive = isActive;
    }

    public static ProductAttributeValue Create(Guid productId, Guid attributeId, string attributeValue, bool isActive, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(attributeValue, nameof(attributeValue));

        return new ProductAttributeValue(Guid.CreateVersion7(), productId, attributeId, attributeValue.Trim(), isActive, createdBy);
    }

    public void Update(string attributeValue, bool isActive, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(attributeValue, nameof(attributeValue));

        AttributeValue = attributeValue.Trim();
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
