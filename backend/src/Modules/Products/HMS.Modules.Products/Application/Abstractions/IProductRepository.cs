using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;

namespace HMS.Modules.Products.Application.Abstractions;

internal interface IProductRepository
{
    Task AddAsync(Product product, CancellationToken cancellationToken);

    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsBySkuAsync(string sku, Guid? excludingId, CancellationToken cancellationToken);

    Task<bool> ExistsByProductCodeAsync(string productCode, Guid? excludingId, CancellationToken cancellationToken);

    /// <summary>Bulk lookup — one query for a whole page's worth of ids, so callers
    /// denormalizing product names onto a list don't issue one GetByIdAsync per row.</summary>
    Task<IReadOnlyList<Product>> GetManyByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(ProductListQuery query, CancellationToken cancellationToken);

    /// <summary>Every active product matching <paramref name="search"/>, name-ordered and
    /// unpaged, projected in SQL to the few columns ProductStockProfileResponse carries.</summary>
    Task<IReadOnlyList<ProductStockProfileResponse>> GetStockProfilesAsync(string? search, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
