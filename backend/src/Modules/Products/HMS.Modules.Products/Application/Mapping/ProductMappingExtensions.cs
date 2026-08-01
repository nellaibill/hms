using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;

namespace HMS.Modules.Products.Application.Mapping;

internal static class ProductMappingExtensions
{
    public static ProductResponse ToResponse(this Product product) => new()
    {
        Id = product.Id,
        Sku = product.Sku,
        ProductCode = product.ProductCode,
        ProductName = product.ProductName,
        GenericName = product.GenericName,
        Description = product.Description,
        BrandId = product.BrandId,
        ManufacturerId = product.ManufacturerId,
        CategoryId = product.CategoryId,
        SubCategoryId = product.SubCategoryId,
        GroupId = product.GroupId,
        UomId = product.UomId,
        BaseUomId = product.BaseUomId,
        IsBatchTracked = product.IsBatchTracked,
        IsSerialized = product.IsSerialized,
        IsActive = product.IsActive,
        ReorderLevel = product.ReorderLevel,
        MinStockLevel = product.MinStockLevel,
        MaxStockLevel = product.MaxStockLevel,
        Mrp = product.Mrp,
        CostPrice = product.CostPrice,
        SellingPrice = product.SellingPrice,
        HsnCode = product.HsnCode,
        Weight = product.Weight,
        Volume = product.Volume,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt,
    };
}
