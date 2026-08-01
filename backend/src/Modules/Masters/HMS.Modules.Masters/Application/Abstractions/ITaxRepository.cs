using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

internal interface ITaxRepository
{
    Task AddAsync(Tax tax, CancellationToken cancellationToken);

    Task<Tax?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string taxCode, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Tax> Items, int TotalCount)> GetPagedAsync(TaxListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
