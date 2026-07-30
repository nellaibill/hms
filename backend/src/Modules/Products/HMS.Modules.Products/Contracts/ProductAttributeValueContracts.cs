using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Contracts;

public record CreateProductAttributeValueRequest
{
    public Guid AttributeId { get; init; }
    public string AttributeValue { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
}

public record UpdateProductAttributeValueRequest
{
    public string AttributeValue { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
}

public record ProductAttributeValueResponse
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public Guid AttributeId { get; init; }
    public string AttributeValue { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class ProductAttributeValueListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
