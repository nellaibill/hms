using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class UnitOfMeasureRepository : IUnitOfMeasureRepository
{
    private readonly MastersDbContext _dbContext;

    public UnitOfMeasureRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(UnitOfMeasure unitOfMeasure, CancellationToken cancellationToken)
        => await _dbContext.UnitsOfMeasure.AddAsync(unitOfMeasure, cancellationToken);

    public Task<UnitOfMeasure?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.UnitsOfMeasure.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string uomCode, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.UnitsOfMeasure.AnyAsync(u => u.UomCode == uomCode && u.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<UnitOfMeasure> Items, int TotalCount)> GetPagedAsync(UnitOfMeasureListQuery query, CancellationToken cancellationToken)
    {
        var uoms = _dbContext.UnitsOfMeasure.AsQueryable();

        if (query.IsActive.HasValue)
        {
            uoms = uoms.Where(u => u.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            uoms = uoms.Where(u => EF.Functions.ILike(u.UomCode, term) || EF.Functions.ILike(u.UomName, term));
        }

        uoms = ApplySort(uoms, query.Sort);

        var totalCount = await uoms.CountAsync(cancellationToken);
        var items = await uoms.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<UnitOfMeasure> ApplySort(IQueryable<UnitOfMeasure> uoms, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return uoms.OrderBy(u => u.UomName);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "uomcode" => descending ? uoms.OrderByDescending(u => u.UomCode) : uoms.OrderBy(u => u.UomCode),
            "updatedat" => descending ? uoms.OrderByDescending(u => u.UpdatedAt) : uoms.OrderBy(u => u.UpdatedAt),
            _ => descending ? uoms.OrderByDescending(u => u.UomName) : uoms.OrderBy(u => u.UomName),
        };
    }
}
