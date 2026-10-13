using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Contracts;

public record CreateProductBatchRequest
{
    public string BatchNo { get; init; } = string.Empty;
    public DateOnly ManufactureDate { get; init; }
    public DateOnly ExpiryDate { get; init; }
    public bool IsActive { get; init; } = true;

    /// <summary>Per pack — see ProductBatch.SetPrices.</summary>
    public decimal? Mrp { get; init; }
    public decimal? PurchaseRate { get; init; }
    public decimal? SellingRate { get; init; }
}

public record UpdateProductBatchRequest
{
    public DateOnly ManufactureDate { get; init; }
    public DateOnly ExpiryDate { get; init; }
    public bool IsActive { get; init; } = true;

    /// <summary>Per pack — see ProductBatch.SetPrices.</summary>
    public decimal? Mrp { get; init; }
    public decimal? PurchaseRate { get; init; }
    public decimal? SellingRate { get; init; }
}

public record ProductBatchResponse
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string BatchNo { get; init; } = string.Empty;
    public DateOnly ManufactureDate { get; init; }
    public DateOnly ExpiryDate { get; init; }
    public bool IsActive { get; init; }

    /// <summary>Per pack. PurchaseRate is null for a caller without pharmacy.view-cost, the
    /// same way ProductResponse.CostPrice is withheld (ADR-077).</summary>
    public decimal? Mrp { get; init; }
    public decimal? PurchaseRate { get; init; }
    public decimal? SellingRate { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class ProductBatchListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
