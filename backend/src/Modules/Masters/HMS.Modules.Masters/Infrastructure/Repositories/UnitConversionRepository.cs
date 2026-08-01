using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class UnitConversionRepository : IUnitConversionRepository
{
    private readonly MastersDbContext _dbContext;

    public UnitConversionRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(UnitConversion conversion, CancellationToken cancellationToken)
        => await _dbContext.UnitConversions.AddAsync(conversion, cancellationToken);

    public Task<UnitConversion?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.UnitConversions.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid fromUomId, Guid toUomId, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.UnitConversions.AnyAsync(u => u.FromUomId == fromUomId && u.ToUomId == toUomId && u.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<UnitConversion> Items, int TotalCount)> GetPagedAsync(UnitConversionListQuery query, CancellationToken cancellationToken)
    {
        var conversions = _dbContext.UnitConversions.AsQueryable();

        if (query.IsActive.HasValue)
        {
            conversions = conversions.Where(u => u.IsActive == query.IsActive.Value);
        }

        conversions = ApplySort(conversions, query.Sort);

        var totalCount = await conversions.CountAsync(cancellationToken);
        var items = await conversions.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<UnitConversion> ApplySort(IQueryable<UnitConversion> conversions, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return conversions.OrderByDescending(u => u.UpdatedAt);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "conversionfactor" => descending ? conversions.OrderByDescending(u => u.ConversionFactor) : conversions.OrderBy(u => u.ConversionFactor),
            _ => descending ? conversions.OrderByDescending(u => u.UpdatedAt) : conversions.OrderBy(u => u.UpdatedAt),
        };
    }
}
