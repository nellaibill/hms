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

    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(ProductListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
