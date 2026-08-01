using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;

namespace HMS.Modules.Products.Application.Abstractions;

internal interface IProductTaxMappingRepository
{
    Task AddAsync(ProductTaxMapping mapping, CancellationToken cancellationToken);

    Task<ProductTaxMapping?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid productId, Guid taxId, string taxType, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<ProductTaxMapping> Items, int TotalCount)> GetPagedByProductAsync(Guid productId, ProductTaxMappingListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
