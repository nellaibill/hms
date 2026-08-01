using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;

namespace HMS.Modules.Products.Application.Abstractions;

internal interface IProductImageRepository
{
    Task AddAsync(ProductImage image, CancellationToken cancellationToken);

    Task<ProductImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid productId, string imageType, int displayOrder, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<ProductImage> Items, int TotalCount)> GetPagedByProductAsync(Guid productId, ProductImageListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
