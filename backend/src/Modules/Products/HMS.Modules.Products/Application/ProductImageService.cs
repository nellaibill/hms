using HMS.Modules.Products.Application.Abstractions;
using HMS.Modules.Products.Application.Mapping;
using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;
using HMS.Shared.Kernel;
using SixLabors.ImageSharp;

namespace HMS.Modules.Products.Application;

public interface IProductImageService
{
    /// <summary>Validates, stores (via IProductImageStorage), and persists an uploaded product image in one step — the only way an image's URL is ever set.</summary>
    Task<Result<ProductImageResponse>> UploadAsync(Guid productId, Stream content, string fileName, long length, string imageType, bool isPrimary, int displayOrder, bool isActive, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ProductImageResponse>> UpdateAsync(Guid productId, Guid id, UpdateProductImageRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ProductImageResponse>> GetByIdAsync(Guid productId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ProductImageResponse>> GetPagedAsync(Guid productId, ProductImageListQuery query, CancellationToken cancellationToken);
}

internal class ProductImageService : IProductImageService
{
    // Matches BrandingService's curated extension list; a generous 5MB cap (vs Branding's
    // 500KB logo cap) since these are full product photos, not a small UI logo.
    private static readonly string[] AllowedImageExtensions = [".png", ".jpg", ".jpeg", ".webp"];
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;

    private readonly IProductImageRepository _repository;
    private readonly IProductRepository _productRepository;
    private readonly IProductImageStorage _storage;

    public ProductImageService(IProductImageRepository repository, IProductRepository productRepository, IProductImageStorage storage)
    {
        _repository = repository;
        _productRepository = productRepository;
        _storage = storage;
    }

    public async Task<Result<ProductImageResponse>> UploadAsync(Guid productId, Stream content, string fileName, long length, string imageType, bool isPrimary, int displayOrder, bool isActive, Guid? actorId, CancellationToken cancellationToken)
    {
        if (!await _productRepository.ExistsAsync(productId, cancellationToken))
        {
            return Result<ProductImageResponse>.Failure(ProductsErrorCodes.InvalidReference, $"Product '{productId}' was not found.");
        }

        if (length > MaxImageSizeBytes)
        {
            return Result<ProductImageResponse>.Failure(ProductsErrorCodes.InvalidReference, "Image must be 5MB or smaller.");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(extension))
        {
            return Result<ProductImageResponse>.Failure(ProductsErrorCodes.InvalidReference, "Image must be a PNG, JPG, or WEBP file.");
        }

        if (await _repository.ExistsAsync(productId, imageType.Trim(), displayOrder, excludingId: null, cancellationToken))
        {
            return Result<ProductImageResponse>.Failure(ProductsErrorCodes.DuplicateCode, "An image with this type and display order already exists for this product.");
        }

        // Buffered once so the actual bytes can be content-sniffed as a real image — never
        // trusting the client-declared extension alone — matching BrandingService/
        // UserService's existing pattern for every other image upload path in this codebase.
        // Storage then reads from this same buffered copy.
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        ImageInfo? info;
        try
        {
            info = Image.Identify(buffer);
        }
        catch
        {
            info = null;
        }
        finally
        {
            buffer.Position = 0;
        }

        if (info is null)
        {
            return Result<ProductImageResponse>.Failure(ProductsErrorCodes.InvalidReference, "The uploaded file is not a valid image.");
        }

        var imageUrl = await _storage.SaveAsync(productId, fileName, buffer, cancellationToken);

        var image = ProductImage.Create(productId, imageUrl, imageType, isPrimary, displayOrder, isActive, actorId);

        await _repository.AddAsync(image, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ProductImageResponse>.Success(image.ToResponse());
    }

    public async Task<Result<ProductImageResponse>> UpdateAsync(Guid productId, Guid id, UpdateProductImageRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var image = await _repository.GetByIdAsync(id, cancellationToken);
        if (image is null || image.ProductId != productId)
        {
            return Result<ProductImageResponse>.Failure(ProductsErrorCodes.NotFound, $"Image '{id}' was not found for product '{productId}'.");
        }

        image.Update(request.ImageType, request.IsPrimary, request.DisplayOrder, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ProductImageResponse>.Success(image.ToResponse());
    }

    public async Task<Result<ProductImageResponse>> GetByIdAsync(Guid productId, Guid id, CancellationToken cancellationToken)
    {
        var image = await _repository.GetByIdAsync(id, cancellationToken);
        return image is null || image.ProductId != productId
            ? Result<ProductImageResponse>.Failure(ProductsErrorCodes.NotFound, $"Image '{id}' was not found for product '{productId}'.")
            : Result<ProductImageResponse>.Success(image.ToResponse());
    }

    public async Task<PagedResult<ProductImageResponse>> GetPagedAsync(Guid productId, ProductImageListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedByProductAsync(productId, query, cancellationToken);
        return new PagedResult<ProductImageResponse>(items.Select(i => i.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
