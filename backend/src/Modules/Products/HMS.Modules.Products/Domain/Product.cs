using HMS.Modules.Products.Contracts;
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

    // Drug profile — see SetDrugProfile.
    public DosageForm? DosageForm { get; private set; }
    public string? Strength { get; private set; }
    public decimal UnitsPerPack { get; private set; } = 1m;
    public DrugSchedule Schedule { get; private set; } = DrugSchedule.None;
    public StorageCondition StorageCondition { get; private set; } = StorageCondition.RoomTemperature;
    public string? RackLocation { get; private set; }
    public decimal? GstRatePercent { get; private set; }

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

    /// <summary>
    /// The pharmacy-specific attributes of a product, set separately from Create/Update so
    /// their already-long parameter lists don't grow further (same shape as
    /// PatientVisit.SetAdmissionDetails).
    /// <para>
    /// <paramref name="unitsPerPack"/> is how many stock units make one pack as bought and
    /// priced — 10 for a strip of ten tablets, 1 for a bottle. Stock quantities are always in
    /// units; MRP and rates are always per pack; a unit's price is the pack price divided by
    /// this. It defaults to 1, where pack and unit are the same thing, which is how every
    /// product behaved before this field existed.
    /// </para>
    /// </summary>
    public void SetDrugProfile(
        DosageForm? dosageForm,
        string? strength,
        decimal unitsPerPack,
        DrugSchedule schedule,
        StorageCondition storageCondition,
        string? rackLocation,
        decimal? gstRatePercent,
        Guid? updatedBy)
    {
        Guard.AgainstNonPositive(unitsPerPack, nameof(unitsPerPack));
        if (gstRatePercent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(gstRatePercent), "GST rate must be between 0 and 100.");
        }

        DosageForm = dosageForm;
        Strength = string.IsNullOrWhiteSpace(strength) ? null : strength.Trim();
        UnitsPerPack = unitsPerPack;
        Schedule = schedule;
        StorageCondition = storageCondition;
        RackLocation = string.IsNullOrWhiteSpace(rackLocation) ? null : rackLocation.Trim();
        GstRatePercent = gstRatePercent;
        MarkUpdated(updatedBy);
    }
}
