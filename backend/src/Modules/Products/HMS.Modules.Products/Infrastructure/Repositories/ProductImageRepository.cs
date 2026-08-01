using HMS.Modules.Products.Application.Abstractions;
using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Products.Infrastructure.Repositories;

internal class ProductImageRepository : IProductImageRepository
{
    private readonly ProductsDbContext _dbContext;

    public ProductImageRepository(ProductsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ProductImage image, CancellationToken cancellationToken)
        => await _dbContext.ProductImages.AddAsync(image, cancellationToken);

    public Task<ProductImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.ProductImages.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid productId, string imageType, int displayOrder, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.ProductImages.AnyAsync(
            i => i.ProductId == productId && i.ImageType == imageType && i.DisplayOrder == displayOrder && i.Id != excludingId,
            cancellationToken);

    public async Task<(IReadOnlyList<ProductImage> Items, int TotalCount)> GetPagedByProductAsync(Guid productId, ProductImageListQuery query, CancellationToken cancellationToken)
    {
        var images = _dbContext.ProductImages.Where(i => i.ProductId == productId);

        if (query.IsActive.HasValue)
        {
            images = images.Where(i => i.IsActive == query.IsActive.Value);
        }

        images = images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder);

        var totalCount = await images.CountAsync(cancellationToken);
        var items = await images.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
