using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Domain;

/// <summary>
/// The product/item master (docs/04_Product_Management_ERD, "Core Product") — the aggregate
/// root every other entity in this module (barcodes, batches, prices, images, attribute
/// values, tax mappings) belongs to via a plain <c>product_id</c> FK. Classification and unit
/// fields (<see cref="CategoryId"/>, <see cref="BrandId"/>, <see cref="UomId"/>, ...) are
/// FK-only references into the Masters module's schema — modeled as plain <see cref="Guid"/>
/// columns (no EF navigation) since the target types live in a different module/assembly.
/// Referential validity against Masters is checked at the Application layer via Masters'
/// public I{Entity}Service seam, not via a database-level cross-schema constraint alone.
/// </summary>
internal class Product : Entity
{
    public string Sku { get; private set; } = null!;
    public string ProductCode { get; private set; } = null!;
    public string ProductName { get; private set; } = null!;
    public string? GenericName { get; private set; }
    public string? Description { get; private set; }

    public Guid BrandId { get; private set; }
    public Guid ManufacturerId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Guid SubCategoryId { get; private set; }
    public Guid GroupId { get; private set; }
    public Guid UomId { get; private set; }
    public Guid BaseUomId { get; private set; }

    public bool IsBatchTracked { get; private set; }
    public bool IsSerialized { get; private set; }
    public bool IsActive { get; private set; } = true;

    public decimal ReorderLevel { get; private set; }
    public decimal MinStockLevel { get; private set; }
    public decimal MaxStockLevel { get; private set; }
    public decimal Mrp { get; private set; }
    public decimal CostPrice { get; private set; }
    public decimal SellingPrice { get; private set; }

    public string? HsnCode { get; private set; }
    public decimal? Weight { get; private set; }
    public decimal? Volume { get; private set; }

    private Product()
    {
    }

    private Product(
        Guid id, string sku, string productCode, string productName, string? genericName, string? description,
        Guid brandId, Guid manufacturerId, Guid categoryId, Guid subCategoryId, Guid groupId, Guid uomId, Guid baseUomId,
        bool isBatchTracked, bool isSerialized, bool isActive,
        decimal reorderLevel, decimal minStockLevel, decimal maxStockLevel, decimal mrp, decimal costPrice, decimal sellingPrice,
        string? hsnCode, decimal? weight, decimal? volume, Guid? createdBy)
        : base(id, createdBy)
    {
        Sku = sku;
        ProductCode = productCode;
        ProductName = productName;
        GenericName = genericName;
        Description = description;
        BrandId = brandId;
        ManufacturerId = manufacturerId;
        CategoryId = categoryId;
        SubCategoryId = subCategoryId;
        GroupId = groupId;
        UomId = uomId;
        BaseUomId = baseUomId;
        IsBatchTracked = isBatchTracked;
        IsSerialized = isSerialized;
        IsActive = isActive;
        ReorderLevel = reorderLevel;
        MinStockLevel = minStockLevel;
        MaxStockLevel = maxStockLevel;
        Mrp = mrp;
        CostPrice = costPrice;
        SellingPrice = sellingPrice;
        HsnCode = hsnCode;
        Weight = weight;
        Volume = volume;
    }

    public static Product Create(
        string sku, string productCode, string productName, string? genericName, string? description,
        Guid brandId, Guid manufacturerId, Guid categoryId, Guid subCategoryId, Guid groupId, Guid uomId, Guid baseUomId,
        bool isBatchTracked, bool isSerialized, bool isActive,
        decimal reorderLevel, decimal minStockLevel, decimal maxStockLevel, decimal mrp, decimal costPrice, decimal sellingPrice,
        string? hsnCode, decimal? weight, decimal? volume, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(sku, nameof(sku));
        Guard.AgainstNullOrWhiteSpace(productCode, nameof(productCode));
        Guard.AgainstNullOrWhiteSpace(productName, nameof(productName));

        return new Product(
            Guid.CreateVersion7(), sku.Trim().ToUpperInvariant(), productCode.Trim().ToUpperInvariant(), productName.Trim(),
            genericName?.Trim(), description?.Trim(),
            brandId, manufacturerId, categoryId, subCategoryId, groupId, uomId, baseUomId,
            isBatchTracked, isSerialized, isActive,
            reorderLevel, minStockLevel, maxStockLevel, mrp, costPrice, sellingPrice,
            hsnCode?.Trim(), weight, volume, createdBy);
    }

    public void Update(
        string productName, string? genericName, string? description,
        Guid brandId, Guid manufacturerId, Guid categoryId, Guid subCategoryId, Guid groupId, Guid uomId, Guid baseUomId,
        bool isBatchTracked, bool isSerialized, bool isActive,
        decimal reorderLevel, decimal minStockLevel, decimal maxStockLevel, decimal mrp, decimal costPrice, decimal sellingPrice,
        string? hsnCode, decimal? weight, decimal? volume, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(productName, nameof(productName));

        ProductName = productName.Trim();
        GenericName = genericName?.Trim();
        Description = description?.Trim();
        BrandId = brandId;
        ManufacturerId = manufacturerId;
        CategoryId = categoryId;
        SubCategoryId = subCategoryId;
        GroupId = groupId;
        UomId = uomId;
        BaseUomId = baseUomId;
        IsBatchTracked = isBatchTracked;
        IsSerialized = isSerialized;
        IsActive = isActive;
        ReorderLevel = reorderLevel;
        MinStockLevel = minStockLevel;
        MaxStockLevel = maxStockLevel;
        Mrp = mrp;
        CostPrice = costPrice;
        SellingPrice = sellingPrice;
        HsnCode = hsnCode?.Trim();
        Weight = weight;
        Volume = volume;
        MarkUpdated(updatedBy);
    }
}
