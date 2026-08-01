using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class CurrencyRepository : ICurrencyRepository
{
    private readonly MastersDbContext _dbContext;

    public CurrencyRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Currency currency, CancellationToken cancellationToken)
        => await _dbContext.Currencies.AddAsync(currency, cancellationToken);

    public Task<Currency?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Currencies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string currencyCode, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.Currencies.AnyAsync(c => c.CurrencyCode == currencyCode && c.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<Currency> Items, int TotalCount)> GetPagedAsync(CurrencyListQuery query, CancellationToken cancellationToken)
    {
        var currencies = _dbContext.Currencies.AsQueryable();

        if (query.IsActive.HasValue)
        {
            currencies = currencies.Where(c => c.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            currencies = currencies.Where(c => EF.Functions.ILike(c.CurrencyCode, term) || EF.Functions.ILike(c.CurrencyName, term));
        }

        currencies = ApplySort(currencies, query.Sort);

        var totalCount = await currencies.CountAsync(cancellationToken);

        var items = await currencies
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Currency> ApplySort(IQueryable<Currency> currencies, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return currencies.OrderBy(c => c.CurrencyName);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "currencycode" => descending ? currencies.OrderByDescending(c => c.CurrencyCode) : currencies.OrderBy(c => c.CurrencyCode),
            "updatedat" => descending ? currencies.OrderByDescending(c => c.UpdatedAt) : currencies.OrderBy(c => c.UpdatedAt),
            _ => descending ? currencies.OrderByDescending(c => c.CurrencyName) : currencies.OrderBy(c => c.CurrencyName),
        };
    }
}
