using HMS.Modules.Products.Application.Abstractions;
using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Products.Infrastructure.Repositories;

internal class ProductRepository : IProductRepository
{
    private readonly ProductsDbContext _dbContext;

    public ProductRepository(ProductsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken)
        => await _dbContext.Products.AddAsync(product, cancellationToken);

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Products.AnyAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ExistsBySkuAsync(string sku, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.Products.AnyAsync(p => p.Sku == sku && p.Id != excludingId, cancellationToken);

    public Task<bool> ExistsByProductCodeAsync(string productCode, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.Products.AnyAsync(p => p.ProductCode == productCode && p.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(ProductListQuery query, CancellationToken cancellationToken)
    {
        var products = _dbContext.Products.AsQueryable();

        if (query.IsActive.HasValue)
        {
            products = products.Where(p => p.IsActive == query.IsActive.Value);
        }

        if (query.CategoryId.HasValue)
        {
            products = products.Where(p => p.CategoryId == query.CategoryId.Value);
        }

        if (query.BrandId.HasValue)
        {
            products = products.Where(p => p.BrandId == query.BrandId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            products = products.Where(p => EF.Functions.ILike(p.Sku, term) || EF.Functions.ILike(p.ProductCode, term) || EF.Functions.ILike(p.ProductName, term));
        }

        products = ApplySort(products, query.Sort);

        var totalCount = await products.CountAsync(cancellationToken);
        var items = await products.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Product> ApplySort(IQueryable<Product> products, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return products.OrderBy(p => p.ProductName);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "sku" => descending ? products.OrderByDescending(p => p.Sku) : products.OrderBy(p => p.Sku),
            "productcode" => descending ? products.OrderByDescending(p => p.ProductCode) : products.OrderBy(p => p.ProductCode),
            "updatedat" => descending ? products.OrderByDescending(p => p.UpdatedAt) : products.OrderBy(p => p.UpdatedAt),
            _ => descending ? products.OrderByDescending(p => p.ProductName) : products.OrderBy(p => p.ProductName),
        };
    }
}
