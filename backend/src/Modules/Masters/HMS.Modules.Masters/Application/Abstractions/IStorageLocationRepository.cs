using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

internal interface IStorageLocationRepository
{
    Task AddAsync(StorageLocation location, CancellationToken cancellationToken);

    Task<StorageLocation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(Guid warehouseId, string locationCode, Guid? excludingId, CancellationToken cancellationToken);

    /// <summary>True if <paramref name="parentLocationId"/> exists and belongs to <paramref name="warehouseId"/> — a parent location must be in the same warehouse.</summary>
    Task<bool> ExistsInWarehouseAsync(Guid parentLocationId, Guid warehouseId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<StorageLocation> Items, int TotalCount)> GetPagedAsync(StorageLocationListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
