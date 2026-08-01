using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Contracts;

public record CreateProductAttributeRequest
{
    public string AttributeCode { get; init; } = string.Empty;
    public string AttributeName { get; init; } = string.Empty;
    public string DataType { get; init; } = string.Empty;
    public bool IsMandatory { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateProductAttributeRequest
{
    public string AttributeName { get; init; } = string.Empty;
    public string DataType { get; init; } = string.Empty;
    public bool IsMandatory { get; init; }
    public bool IsActive { get; init; } = true;
}

public record ProductAttributeResponse
{
    public Guid Id { get; init; }
    public string AttributeCode { get; init; } = string.Empty;
    public string AttributeName { get; init; } = string.Empty;
    public string DataType { get; init; } = string.Empty;
    public bool IsMandatory { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class ProductAttributeListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
