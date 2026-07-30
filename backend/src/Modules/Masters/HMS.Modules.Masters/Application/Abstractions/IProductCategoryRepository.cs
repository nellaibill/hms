using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

internal interface IProductCategoryRepository
{
    Task AddAsync(ProductCategory category, CancellationToken cancellationToken);

    Task<ProductCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string categoryCode, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<ProductCategory> Items, int TotalCount)> GetPagedAsync(ProductCategoryListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
