using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class StorageLocationRepository : IStorageLocationRepository
{
    private readonly MastersDbContext _dbContext;

    public StorageLocationRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(StorageLocation location, CancellationToken cancellationToken)
        => await _dbContext.StorageLocations.AddAsync(location, cancellationToken);

    public Task<StorageLocation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.StorageLocations.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(Guid warehouseId, string locationCode, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.StorageLocations.AnyAsync(s => s.WarehouseId == warehouseId && s.LocationCode == locationCode && s.Id != excludingId, cancellationToken);

    public Task<bool> ExistsInWarehouseAsync(Guid parentLocationId, Guid warehouseId, CancellationToken cancellationToken)
        => _dbContext.StorageLocations.AnyAsync(s => s.Id == parentLocationId && s.WarehouseId == warehouseId, cancellationToken);

    public async Task<(IReadOnlyList<StorageLocation> Items, int TotalCount)> GetPagedAsync(StorageLocationListQuery query, CancellationToken cancellationToken)
    {
        var locations = _dbContext.StorageLocations.AsQueryable();

        if (query.IsActive.HasValue)
        {
            locations = locations.Where(s => s.IsActive == query.IsActive.Value);
        }

        if (query.WarehouseId.HasValue)
        {
            locations = locations.Where(s => s.WarehouseId == query.WarehouseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            locations = locations.Where(s => EF.Functions.ILike(s.LocationCode, term) || EF.Functions.ILike(s.LocationName, term));
        }

        locations = ApplySort(locations, query.Sort);

        var totalCount = await locations.CountAsync(cancellationToken);
        var items = await locations.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<StorageLocation> ApplySort(IQueryable<StorageLocation> locations, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return locations.OrderBy(s => s.LocationName);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "locationcode" => descending ? locations.OrderByDescending(s => s.LocationCode) : locations.OrderBy(s => s.LocationCode),
            "updatedat" => descending ? locations.OrderByDescending(s => s.UpdatedAt) : locations.OrderBy(s => s.UpdatedAt),
            _ => descending ? locations.OrderByDescending(s => s.LocationName) : locations.OrderBy(s => s.LocationName),
        };
    }
}
