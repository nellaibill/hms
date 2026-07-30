using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

internal interface IProductGroupRepository
{
    Task AddAsync(ProductGroup group, CancellationToken cancellationToken);

    Task<ProductGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string groupCode, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<ProductGroup> Items, int TotalCount)> GetPagedAsync(ProductGroupListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
