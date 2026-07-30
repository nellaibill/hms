using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Contracts;

public record CreateProductRequest
{
    public string Sku { get; init; } = string.Empty;
    public string ProductCode { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string? GenericName { get; init; }
    public string? Description { get; init; }
    public Guid BrandId { get; init; }
    public Guid ManufacturerId { get; init; }
    public Guid CategoryId { get; init; }
    public Guid SubCategoryId { get; init; }
    public Guid GroupId { get; init; }
    public Guid UomId { get; init; }
    public Guid BaseUomId { get; init; }
    public bool IsBatchTracked { get; init; }
    public bool IsSerialized { get; init; }
    public bool IsActive { get; init; } = true;
    public decimal ReorderLevel { get; init; }
    public decimal MinStockLevel { get; init; }
    public decimal MaxStockLevel { get; init; }
    public decimal Mrp { get; init; }
    public decimal CostPrice { get; init; }
    public decimal SellingPrice { get; init; }
    public string? HsnCode { get; init; }
    public decimal? Weight { get; init; }
    public decimal? Volume { get; init; }
}

public record UpdateProductRequest
{
    public string ProductName { get; init; } = string.Empty;
    public string? GenericName { get; init; }
    public string? Description { get; init; }
    public Guid BrandId { get; init; }
    public Guid ManufacturerId { get; init; }
    public Guid CategoryId { get; init; }
    public Guid SubCategoryId { get; init; }
    public Guid GroupId { get; init; }
    public Guid UomId { get; init; }
    public Guid BaseUomId { get; init; }
    public bool IsBatchTracked { get; init; }
    public bool IsSerialized { get; init; }
    public bool IsActive { get; init; } = true;
    public decimal ReorderLevel { get; init; }
    public decimal MinStockLevel { get; init; }
    public decimal MaxStockLevel { get; init; }
    public decimal Mrp { get; init; }
    public decimal CostPrice { get; init; }
    public decimal SellingPrice { get; init; }
    public string? HsnCode { get; init; }
    public decimal? Weight { get; init; }
    public decimal? Volume { get; init; }
}

public record ProductResponse
{
    public Guid Id { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string ProductCode { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string? GenericName { get; init; }
    public string? Description { get; init; }
    public Guid BrandId { get; init; }
    public Guid ManufacturerId { get; init; }
    public Guid CategoryId { get; init; }
    public Guid SubCategoryId { get; init; }
    public Guid GroupId { get; init; }
    public Guid UomId { get; init; }
    public Guid BaseUomId { get; init; }
    public bool IsBatchTracked { get; init; }
    public bool IsSerialized { get; init; }
    public bool IsActive { get; init; }
    public decimal ReorderLevel { get; init; }
    public decimal MinStockLevel { get; init; }
    public decimal MaxStockLevel { get; init; }
    public decimal Mrp { get; init; }
    public decimal CostPrice { get; init; }
    public decimal SellingPrice { get; init; }
    public string? HsnCode { get; init; }
    public decimal? Weight { get; init; }
    public decimal? Volume { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class ProductListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
}
