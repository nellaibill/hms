using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

internal interface IWarehouseRepository
{
    Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken);

    Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string warehouseCode, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Warehouse> Items, int TotalCount)> GetPagedAsync(WarehouseListQuery query, CancellationToken cancellationToken);

    /// <summary>All active warehouses, unpaged — used to populate Storage Location's Warehouse reference dropdown.</summary>
    Task<IReadOnlyList<Warehouse>> GetAllAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
