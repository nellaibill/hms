using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>Product brand master used across Inventory &amp; ERP (docs/03_Masters_ERD, "Brand &amp; Manufacturer").</summary>
internal class Brand : Entity
{
    public string BrandCode { get; private set; } = null!;
    public string BrandName { get; private set; } = null!;
    public string? Description { get; private set; }

    public bool IsActive { get; private set; } = true;

    private Brand()
    {
    }

    private Brand(Guid id, string brandCode, string brandName, string? description, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        BrandCode = brandCode;
        BrandName = brandName;
        Description = description;
        IsActive = isActive;
    }

    public static Brand Create(string brandCode, string brandName, string? description, bool isActive, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(brandCode, nameof(brandCode));
        Guard.AgainstNullOrWhiteSpace(brandName, nameof(brandName));

        return new Brand(Guid.CreateVersion7(), brandCode.Trim().ToUpperInvariant(), brandName.Trim(), description?.Trim(), isActive, createdBy);
    }

    public void Update(string brandName, string? description, bool isActive, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(brandName, nameof(brandName));

        BrandName = brandName.Trim();
        Description = description?.Trim();
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
