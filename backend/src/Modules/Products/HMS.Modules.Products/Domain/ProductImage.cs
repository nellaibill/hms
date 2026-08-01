using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Domain;

/// <summary>A product image slot (Main/Side/Label, per docs/04_Product_Management_ERD). <see cref="ImageUrl"/> is populated by the upload endpoint (see IProductImageStorage), not entered directly by callers of Create/Update.</summary>
internal class ProductImage : Entity
{
    public Guid ProductId { get; private set; }
    public string ImageUrl { get; private set; } = null!;
    public string ImageType { get; private set; } = null!;
    public bool IsPrimary { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private ProductImage()
    {
    }

    private ProductImage(Guid id, Guid productId, string imageUrl, string imageType, bool isPrimary, int displayOrder, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        ProductId = productId;
        ImageUrl = imageUrl;
        ImageType = imageType;
        IsPrimary = isPrimary;
        DisplayOrder = displayOrder;
        IsActive = isActive;
    }

    public static ProductImage Create(Guid productId, string imageUrl, string imageType, bool isPrimary, int displayOrder, bool isActive, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(imageUrl, nameof(imageUrl));
        Guard.AgainstNullOrWhiteSpace(imageType, nameof(imageType));

        return new ProductImage(Guid.CreateVersion7(), productId, imageUrl.Trim(), imageType.Trim(), isPrimary, displayOrder, isActive, createdBy);
    }

    public void Update(string imageType, bool isPrimary, int displayOrder, bool isActive, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(imageType, nameof(imageType));

        ImageType = imageType.Trim();
        IsPrimary = isPrimary;
        DisplayOrder = displayOrder;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
