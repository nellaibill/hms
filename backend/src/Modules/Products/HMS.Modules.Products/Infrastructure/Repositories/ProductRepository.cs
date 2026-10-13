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

    public async Task<IReadOnlyList<Product>> GetManyByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        // Copied to a List so the SQL parameter is a type Npgsql maps to an array whatever
        // collection the caller passed (same as RoleRepository.GetManyByIdsAsync).
        var distinctIds = ids.Distinct().ToList();
        return await _dbContext.Products.Where(p => distinctIds.Contains(p.Id)).ToListAsync(cancellationToken);
    }

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

        products = ApplySearch(products, query.Search);
        products = ApplySort(products, query.Sort);

        var totalCount = await products.CountAsync(cancellationToken);
        var items = await products.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<ProductStockProfileResponse>> GetStockProfilesAsync(string? search, CancellationToken cancellationToken)
        => await ApplySearch(_dbContext.Products.Where(p => p.IsActive), search)
            .OrderBy(p => p.ProductName)
            .Select(p => new ProductStockProfileResponse
            {
                Id = p.Id,
                Sku = p.Sku,
                ProductName = p.ProductName,
                GenericName = p.GenericName,
                ReorderLevel = p.ReorderLevel,
            })
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    // GenericName is searched too: a pharmacist looks a drug up by its molecule
    // ("paracetamol") at least as often as by its brand name.
    private IQueryable<Product> ApplySearch(IQueryable<Product> products, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return products;
        }

        // A scanned barcode arrives here as the search text, so an exact barcode match counts
        // as a hit alongside the name/generic/code matches.
        var exact = search.Trim();
        var term = $"%{exact}%";
        return products.Where(p =>
            EF.Functions.ILike(p.Sku, term)
            || EF.Functions.ILike(p.ProductCode, term)
            || EF.Functions.ILike(p.ProductName, term)
            || (p.GenericName != null && EF.Functions.ILike(p.GenericName, term))
            || _dbContext.ProductBarcodes.Any(b => b.ProductId == p.Id && b.IsActive && b.BarcodeValue == exact));
    }

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
