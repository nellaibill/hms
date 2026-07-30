using HMS.Modules.Products.Application.Abstractions;
using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Products.Infrastructure.Repositories;

internal class ProductTaxMappingRepository : IProductTaxMappingRepository
{
    private readonly ProductsDbContext _dbContext;

    public ProductTaxMappingRepository(ProductsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ProductTaxMapping mapping, CancellationToken cancellationToken)
        => await _dbContext.ProductTaxMappings.AddAsync(mapping, cancellationToken);

    public Task<ProductTaxMapping?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.ProductTaxMappings.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid productId, Guid taxId, string taxType, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.ProductTaxMappings.AnyAsync(t => t.ProductId == productId && t.TaxId == taxId && t.TaxType == taxType && t.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<ProductTaxMapping> Items, int TotalCount)> GetPagedByProductAsync(Guid productId, ProductTaxMappingListQuery query, CancellationToken cancellationToken)
    {
        var mappings = _dbContext.ProductTaxMappings.Where(t => t.ProductId == productId);

        if (query.IsActive.HasValue)
        {
            mappings = mappings.Where(t => t.IsActive == query.IsActive.Value);
        }

        mappings = mappings.OrderBy(t => t.TaxType);

        var totalCount = await mappings.CountAsync(cancellationToken);
        var items = await mappings.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
