using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Domain;

/// <summary>
/// A scannable code for a product (docs/04_Product_Management_ERD, business rule 2:
/// "Barcode_value must be unique across all products" — hence the global, not
/// per-product-scoped, unique index in <see cref="Infrastructure.Configurations.ProductBarcodeConfiguration"/>).
/// </summary>
internal class ProductBarcode : Entity
{
    public Guid ProductId { get; private set; }
    public string BarcodeType { get; private set; } = null!;
    public string BarcodeValue { get; private set; } = null!;
    public bool IsPrimary { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? Notes { get; private set; }

    private ProductBarcode()
    {
    }

    private ProductBarcode(Guid id, Guid productId, string barcodeType, string barcodeValue, bool isPrimary, bool isActive, string? notes, Guid? createdBy)
        : base(id, createdBy)
    {
        ProductId = productId;
        BarcodeType = barcodeType;
        BarcodeValue = barcodeValue;
        IsPrimary = isPrimary;
        IsActive = isActive;
        Notes = notes;
    }

    public static ProductBarcode Create(Guid productId, string barcodeType, string barcodeValue, bool isPrimary, bool isActive, string? notes, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(barcodeType, nameof(barcodeType));
        Guard.AgainstNullOrWhiteSpace(barcodeValue, nameof(barcodeValue));

        return new ProductBarcode(Guid.CreateVersion7(), productId, barcodeType.Trim(), barcodeValue.Trim(), isPrimary, isActive, notes?.Trim(), createdBy);
    }

    public void Update(string barcodeType, bool isPrimary, bool isActive, string? notes, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(barcodeType, nameof(barcodeType));

        BarcodeType = barcodeType.Trim();
        IsPrimary = isPrimary;
        IsActive = isActive;
        Notes = notes?.Trim();
        MarkUpdated(updatedBy);
    }
}
