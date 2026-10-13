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

    // Prices are per pack (see Product.SetDrugProfile on packs and units). All nullable:
    // a batch entered before prices were tracked per batch has none, and pricing falls back
    // to the product's own Mrp/CostPrice/SellingPrice.
    public decimal? Mrp { get; private set; }
    public decimal? PurchaseRate { get; private set; }
    public decimal? SellingRate { get; private set; }

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

    /// <summary>
    /// MRP is what's printed on this batch's pack, so it belongs to the batch rather than the
    /// product: the same medicine is routinely on the shelf at two MRPs at once. A selling
    /// rate above MRP is refused — charging more than the printed MRP is unlawful, so it is
    /// never a valid state to store.
    /// </summary>
    public void SetPrices(decimal? mrp, decimal? purchaseRate, decimal? sellingRate, Guid? updatedBy)
    {
        if (mrp < 0) throw new ArgumentOutOfRangeException(nameof(mrp), "MRP cannot be negative.");
        if (purchaseRate < 0) throw new ArgumentOutOfRangeException(nameof(purchaseRate), "Purchase rate cannot be negative.");
        if (sellingRate < 0) throw new ArgumentOutOfRangeException(nameof(sellingRate), "Selling rate cannot be negative.");
        if (mrp is not null && sellingRate > mrp)
        {
            throw new ArgumentOutOfRangeException(nameof(sellingRate), "Selling rate cannot be above MRP.");
        }

        Mrp = mrp;
        PurchaseRate = purchaseRate;
        SellingRate = sellingRate;
        MarkUpdated(updatedBy);
    }
}
