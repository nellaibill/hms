using HMS.Modules.Products.Application.Abstractions;
using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Products.Infrastructure.Repositories;

internal class ProductPriceRepository : IProductPriceRepository
{
    private readonly ProductsDbContext _dbContext;

    public ProductPriceRepository(ProductsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ProductPrice price, CancellationToken cancellationToken)
        => await _dbContext.ProductPrices.AddAsync(price, cancellationToken);

    public Task<ProductPrice?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.ProductPrices.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid productId, string priceType, Guid currencyId, DateOnly effectiveFrom, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.ProductPrices.AnyAsync(
            p => p.ProductId == productId && p.PriceType == priceType && p.CurrencyId == currencyId && p.EffectiveFrom == effectiveFrom && p.Id != excludingId,
            cancellationToken);

    public async Task<(IReadOnlyList<ProductPrice> Items, int TotalCount)> GetPagedByProductAsync(Guid productId, ProductPriceListQuery query, CancellationToken cancellationToken)
    {
        var prices = _dbContext.ProductPrices.Where(p => p.ProductId == productId);

        if (query.IsActive.HasValue)
        {
            prices = prices.Where(p => p.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.PriceType))
        {
            prices = prices.Where(p => p.PriceType == query.PriceType);
        }

        prices = prices.OrderByDescending(p => p.EffectiveFrom);

        var totalCount = await prices.CountAsync(cancellationToken);
        var items = await prices.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
