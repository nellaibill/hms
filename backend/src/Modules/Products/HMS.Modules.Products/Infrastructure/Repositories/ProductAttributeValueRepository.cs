using HMS.Modules.Products.Application.Abstractions;
using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Products.Infrastructure.Repositories;

internal class ProductAttributeValueRepository : IProductAttributeValueRepository
{
    private readonly ProductsDbContext _dbContext;

    public ProductAttributeValueRepository(ProductsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ProductAttributeValue value, CancellationToken cancellationToken)
        => await _dbContext.ProductAttributeValues.AddAsync(value, cancellationToken);

    public Task<ProductAttributeValue?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.ProductAttributeValues.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid productId, Guid attributeId, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.ProductAttributeValues.AnyAsync(v => v.ProductId == productId && v.AttributeId == attributeId && v.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<ProductAttributeValue> Items, int TotalCount)> GetPagedByProductAsync(Guid productId, ProductAttributeValueListQuery query, CancellationToken cancellationToken)
    {
        var values = _dbContext.ProductAttributeValues.Where(v => v.ProductId == productId);

        if (query.IsActive.HasValue)
        {
            values = values.Where(v => v.IsActive == query.IsActive.Value);
        }

        values = values.OrderBy(v => v.AttributeId);

        var totalCount = await values.CountAsync(cancellationToken);
        var items = await values.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
