using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;

namespace HMS.Modules.Products.Application.Abstractions;

internal interface IProductPriceRepository
{
    Task AddAsync(ProductPrice price, CancellationToken cancellationToken);

    Task<ProductPrice?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid productId, string priceType, Guid currencyId, DateOnly effectiveFrom, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<ProductPrice> Items, int TotalCount)> GetPagedByProductAsync(Guid productId, ProductPriceListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
