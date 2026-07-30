using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class WarehouseRepository : IWarehouseRepository
{
    private readonly MastersDbContext _dbContext;

    public WarehouseRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken)
        => await _dbContext.Warehouses.AddAsync(warehouse, cancellationToken);

    public Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Warehouses.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string warehouseCode, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.Warehouses.AnyAsync(w => w.WarehouseCode == warehouseCode && w.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<Warehouse> Items, int TotalCount)> GetPagedAsync(WarehouseListQuery query, CancellationToken cancellationToken)
    {
        var warehouses = _dbContext.Warehouses.AsQueryable();

        if (query.IsActive.HasValue)
        {
            warehouses = warehouses.Where(w => w.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            warehouses = warehouses.Where(w => EF.Functions.ILike(w.WarehouseCode, term) || EF.Functions.ILike(w.WarehouseName, term));
        }

        warehouses = ApplySort(warehouses, query.Sort);

        var totalCount = await warehouses.CountAsync(cancellationToken);
        var items = await warehouses.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<IReadOnlyList<Warehouse>> GetAllAsync(CancellationToken cancellationToken)
        => _dbContext.Warehouses.Where(w => w.IsActive).OrderBy(w => w.WarehouseName).ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<Warehouse>)t.Result, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Warehouse> ApplySort(IQueryable<Warehouse> warehouses, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return warehouses.OrderBy(w => w.WarehouseName);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "warehousecode" => descending ? warehouses.OrderByDescending(w => w.WarehouseCode) : warehouses.OrderBy(w => w.WarehouseCode),
            "updatedat" => descending ? warehouses.OrderByDescending(w => w.UpdatedAt) : warehouses.OrderBy(w => w.UpdatedAt),
            _ => descending ? warehouses.OrderByDescending(w => w.WarehouseName) : warehouses.OrderBy(w => w.WarehouseName),
        };
    }
}
