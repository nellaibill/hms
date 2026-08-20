using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Pharmacy.Infrastructure.Repositories;

internal class PharmacyStockBalanceRepository : IPharmacyStockBalanceRepository
{
    private readonly PharmacyDbContext _dbContext;

    public PharmacyStockBalanceRepository(PharmacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PharmacyStockBalance balance, CancellationToken cancellationToken)
        => await _dbContext.StockBalances.AddAsync(balance, cancellationToken);

    public Task<PharmacyStockBalance?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.StockBalances.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<PharmacyStockBalance?> GetByProductAndBatchAsync(Guid productId, Guid productBatchId, CancellationToken cancellationToken)
    {
        var balance = await _dbContext.StockBalances
            .FirstOrDefaultAsync(b => b.ProductId == productId && b.ProductBatchId == productBatchId, cancellationToken);

        if (balance is not null && _dbContext.Entry(balance).State != EntityState.Detached)
        {
            // DispenseService's concurrency retry loop (see its own doc comment) calls this
            // method again after a DbUpdateConcurrencyException specifically to see the
            // now-current on-hand quantity and xmin token. Without this, EF Core's identity
            // map would just hand back the SAME already-tracked instance carrying our
            // uncommitted in-memory Dispense() mutation and stale token — re-querying alone
            // would look "fresh" but silently return stale data. ReloadAsync forces the
            // tracked entity's current values back to what's actually committed in the
            // database.
            await _dbContext.Entry(balance).ReloadAsync(cancellationToken);
        }

        return balance;
    }

    public async Task<(IReadOnlyList<PharmacyStockBalance> Items, int TotalCount)> GetPagedAsync(StockBalanceListQuery query, CancellationToken cancellationToken)
    {
        var balances = _dbContext.StockBalances.AsQueryable();

        if (query.ProductId.HasValue)
        {
            balances = balances.Where(b => b.ProductId == query.ProductId.Value);
        }

        balances = ApplySort(balances, query.Sort);

        var totalCount = await balances.CountAsync(cancellationToken);
        var items = await balances.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<PharmacyStockBalance> ApplySort(IQueryable<PharmacyStockBalance> balances, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return balances.OrderByDescending(b => b.UpdatedAt ?? b.CreatedAt);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "quantityonhand" => descending ? balances.OrderByDescending(b => b.QuantityOnHand) : balances.OrderBy(b => b.QuantityOnHand),
            "createdat" => descending ? balances.OrderByDescending(b => b.CreatedAt) : balances.OrderBy(b => b.CreatedAt),
            _ => descending ? balances.OrderByDescending(b => b.UpdatedAt ?? b.CreatedAt) : balances.OrderBy(b => b.UpdatedAt ?? b.CreatedAt),
        };
    }
}
