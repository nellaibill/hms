using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Domain;

/// <summary>
/// A dynamic attribute definition (docs/04_Product_Management_ERD, business rule 5:
/// "Attributes are dynamic and configurable") — the global catalog entities reference by id
/// from <see cref="ProductAttributeValue"/>. Not product-scoped itself.
/// </summary>
internal class ProductAttribute : Entity
{
    public string AttributeCode { get; private set; } = null!;
    public string AttributeName { get; private set; } = null!;
    public string DataType { get; private set; } = null!;
    public bool IsMandatory { get; private set; }
    public bool IsActive { get; private set; } = true;

    private ProductAttribute()
    {
    }

    private ProductAttribute(Guid id, string attributeCode, string attributeName, string dataType, bool isMandatory, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        AttributeCode = attributeCode;
        AttributeName = attributeName;
        DataType = dataType;
        IsMandatory = isMandatory;
        IsActive = isActive;
    }

    public static ProductAttribute Create(string attributeCode, string attributeName, string dataType, bool isMandatory, bool isActive, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(attributeCode, nameof(attributeCode));
        Guard.AgainstNullOrWhiteSpace(attributeName, nameof(attributeName));
        Guard.AgainstNullOrWhiteSpace(dataType, nameof(dataType));

        return new ProductAttribute(Guid.CreateVersion7(), attributeCode.Trim().ToUpperInvariant(), attributeName.Trim(), dataType.Trim(), isMandatory, isActive, createdBy);
    }

    public void Update(string attributeName, string dataType, bool isMandatory, bool isActive, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(attributeName, nameof(attributeName));
        Guard.AgainstNullOrWhiteSpace(dataType, nameof(dataType));

        AttributeName = attributeName.Trim();
        DataType = dataType.Trim();
        IsMandatory = isMandatory;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
