using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Domain;

/// <summary>Batch/expiry tracking for a batch-tracked product (docs/04_Product_Management_ERD, business rule 3: "Batch_no must be unique for a product").</summary>
internal class ProductBatch : Entity
{
    public Guid ProductId { get; private set; }
    public string BatchNo { get; private set; } = null!;
    public DateOnly ManufactureDate { get; private set; }
    public DateOnly ExpiryDate { get; private set; }
    public bool IsActive { get; private set; } = true;

    private ProductBatch()
    {
    }

    private ProductBatch(Guid id, Guid productId, string batchNo, DateOnly manufactureDate, DateOnly expiryDate, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        ProductId = productId;
        BatchNo = batchNo;
        ManufactureDate = manufactureDate;
        ExpiryDate = expiryDate;
        IsActive = isActive;
    }

    public static ProductBatch Create(Guid productId, string batchNo, DateOnly manufactureDate, DateOnly expiryDate, bool isActive, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(batchNo, nameof(batchNo));

        return new ProductBatch(Guid.CreateVersion7(), productId, batchNo.Trim(), manufactureDate, expiryDate, isActive, createdBy);
    }

    public void Update(DateOnly manufactureDate, DateOnly expiryDate, bool isActive, Guid? updatedBy)
    {
        ManufactureDate = manufactureDate;
        ExpiryDate = expiryDate;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
