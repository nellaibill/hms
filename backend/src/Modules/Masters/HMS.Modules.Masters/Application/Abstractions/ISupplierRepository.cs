using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

internal interface ISupplierRepository
{
    Task AddAsync(Supplier supplier, CancellationToken cancellationToken);

    Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string supplierCode, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Supplier> Items, int TotalCount)> GetPagedAsync(SupplierListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
