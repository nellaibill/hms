using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Contracts;

/// <summary>
/// No CreateProductImageRequest exists — ImageUrl is only ever set by the upload endpoint
/// (multipart form fields, see ProductImagesController.Upload), never accepted as an
/// arbitrary client-supplied URL. Update covers everything else.
/// </summary>
public record UpdateProductImageRequest
{
    public string ImageType { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; } = true;
}

public record ProductImageResponse
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public string ImageType { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class ProductImageListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
