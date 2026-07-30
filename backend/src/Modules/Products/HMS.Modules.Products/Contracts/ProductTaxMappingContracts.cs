using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Contracts;

public record CreateProductTaxMappingRequest
{
    public Guid TaxId { get; init; }
    public string TaxType { get; init; } = string.Empty;
    public bool IsInclusive { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateProductTaxMappingRequest
{
    public bool IsInclusive { get; init; }
    public bool IsActive { get; init; } = true;
}

public record ProductTaxMappingResponse
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public Guid TaxId { get; init; }
    public string TaxType { get; init; } = string.Empty;
    public bool IsInclusive { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class ProductTaxMappingListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
