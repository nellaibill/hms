using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class TaxRepository : ITaxRepository
{
    private readonly MastersDbContext _dbContext;

    public TaxRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Tax tax, CancellationToken cancellationToken)
        => await _dbContext.Taxes.AddAsync(tax, cancellationToken);

    public Task<Tax?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Taxes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string taxCode, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.Taxes.AnyAsync(t => t.TaxCode == taxCode && t.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<Tax> Items, int TotalCount)> GetPagedAsync(TaxListQuery query, CancellationToken cancellationToken)
    {
        var taxes = _dbContext.Taxes.AsQueryable();

        if (query.IsActive.HasValue)
        {
            taxes = taxes.Where(t => t.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            taxes = taxes.Where(t => EF.Functions.ILike(t.TaxCode, term) || EF.Functions.ILike(t.TaxName, term));
        }

        taxes = ApplySort(taxes, query.Sort);

        var totalCount = await taxes.CountAsync(cancellationToken);
        var items = await taxes.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Tax> ApplySort(IQueryable<Tax> taxes, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return taxes.OrderBy(t => t.TaxName);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "taxcode" => descending ? taxes.OrderByDescending(t => t.TaxCode) : taxes.OrderBy(t => t.TaxCode),
            "ratepercent" => descending ? taxes.OrderByDescending(t => t.RatePercent) : taxes.OrderBy(t => t.RatePercent),
            "updatedat" => descending ? taxes.OrderByDescending(t => t.UpdatedAt) : taxes.OrderBy(t => t.UpdatedAt),
            _ => descending ? taxes.OrderByDescending(t => t.TaxName) : taxes.OrderBy(t => t.TaxName),
        };
    }
}
