using HMS.Modules.Products.Application.Abstractions;
using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Products.Infrastructure.Repositories;

internal class ProductBatchRepository : IProductBatchRepository
{
    private readonly ProductsDbContext _dbContext;

    public ProductBatchRepository(ProductsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ProductBatch batch, CancellationToken cancellationToken)
        => await _dbContext.ProductBatches.AddAsync(batch, cancellationToken);

    public Task<ProductBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.ProductBatches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<bool> ExistsByBatchNoAsync(Guid productId, string batchNo, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.ProductBatches.AnyAsync(b => b.ProductId == productId && b.BatchNo == batchNo && b.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<ProductBatch> Items, int TotalCount)> GetPagedByProductAsync(Guid productId, ProductBatchListQuery query, CancellationToken cancellationToken)
    {
        var batches = _dbContext.ProductBatches.Where(b => b.ProductId == productId);

        if (query.IsActive.HasValue)
        {
            batches = batches.Where(b => b.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            batches = batches.Where(b => EF.Functions.ILike(b.BatchNo, term));
        }

        batches = batches.OrderBy(b => b.ExpiryDate);

        var totalCount = await batches.CountAsync(cancellationToken);
        var items = await batches.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
