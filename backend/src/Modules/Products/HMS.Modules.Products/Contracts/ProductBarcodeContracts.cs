using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Contracts;

public record CreateProductBarcodeRequest
{
    public string BarcodeType { get; init; } = string.Empty;
    public string BarcodeValue { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    public bool IsActive { get; init; } = true;
    public string? Notes { get; init; }
}

public record UpdateProductBarcodeRequest
{
    public string BarcodeType { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    public bool IsActive { get; init; } = true;
    public string? Notes { get; init; }
}

public record ProductBarcodeResponse
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string BarcodeType { get; init; } = string.Empty;
    public string BarcodeValue { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    public bool IsActive { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class ProductBarcodeListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
