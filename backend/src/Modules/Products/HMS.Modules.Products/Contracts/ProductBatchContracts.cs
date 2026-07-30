using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Contracts;

public record CreateProductBatchRequest
{
    public string BatchNo { get; init; } = string.Empty;
    public DateOnly ManufactureDate { get; init; }
    public DateOnly ExpiryDate { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateProductBatchRequest
{
    public DateOnly ManufactureDate { get; init; }
    public DateOnly ExpiryDate { get; init; }
    public bool IsActive { get; init; } = true;
}

public record ProductBatchResponse
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string BatchNo { get; init; } = string.Empty;
    public DateOnly ManufactureDate { get; init; }
    public DateOnly ExpiryDate { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class ProductBatchListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
