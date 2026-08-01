using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class ManufacturerRepository : IManufacturerRepository
{
    private readonly MastersDbContext _dbContext;

    public ManufacturerRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Manufacturer manufacturer, CancellationToken cancellationToken)
        => await _dbContext.Manufacturers.AddAsync(manufacturer, cancellationToken);

    public Task<Manufacturer?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Manufacturers.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string manufacturerCode, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.Manufacturers.AnyAsync(m => m.ManufacturerCode == manufacturerCode && m.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<Manufacturer> Items, int TotalCount)> GetPagedAsync(ManufacturerListQuery query, CancellationToken cancellationToken)
    {
        var manufacturers = _dbContext.Manufacturers.AsQueryable();

        if (query.IsActive.HasValue)
        {
            manufacturers = manufacturers.Where(m => m.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            manufacturers = manufacturers.Where(m => EF.Functions.ILike(m.ManufacturerCode, term) || EF.Functions.ILike(m.ManufacturerName, term));
        }

        manufacturers = ApplySort(manufacturers, query.Sort);

        var totalCount = await manufacturers.CountAsync(cancellationToken);
        var items = await manufacturers.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Manufacturer> ApplySort(IQueryable<Manufacturer> manufacturers, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return manufacturers.OrderBy(m => m.ManufacturerName);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "manufacturercode" => descending ? manufacturers.OrderByDescending(m => m.ManufacturerCode) : manufacturers.OrderBy(m => m.ManufacturerCode),
            "updatedat" => descending ? manufacturers.OrderByDescending(m => m.UpdatedAt) : manufacturers.OrderBy(m => m.UpdatedAt),
            _ => descending ? manufacturers.OrderByDescending(m => m.ManufacturerName) : manufacturers.OrderBy(m => m.ManufacturerName),
        };
    }
}
