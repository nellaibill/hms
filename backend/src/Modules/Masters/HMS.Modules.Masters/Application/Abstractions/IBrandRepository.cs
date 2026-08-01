using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

internal interface IBrandRepository
{
    Task AddAsync(Brand brand, CancellationToken cancellationToken);

    Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string brandCode, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Brand> Items, int TotalCount)> GetPagedAsync(BrandListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
