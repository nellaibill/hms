using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class StockAdjustmentReasonRepository : IStockAdjustmentReasonRepository
{
    private readonly MastersDbContext _dbContext;

    public StockAdjustmentReasonRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(StockAdjustmentReason reason, CancellationToken cancellationToken)
        => await _dbContext.StockAdjustmentReasons.AddAsync(reason, cancellationToken);

    public Task<StockAdjustmentReason?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.StockAdjustmentReasons.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string reasonCode, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.StockAdjustmentReasons.AnyAsync(s => s.ReasonCode == reasonCode && s.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<StockAdjustmentReason> Items, int TotalCount)> GetPagedAsync(StockAdjustmentReasonListQuery query, CancellationToken cancellationToken)
    {
        var reasons = _dbContext.StockAdjustmentReasons.AsQueryable();

        if (query.IsActive.HasValue)
        {
            reasons = reasons.Where(s => s.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            reasons = reasons.Where(s => EF.Functions.ILike(s.ReasonCode, term) || EF.Functions.ILike(s.ReasonName, term));
        }

        reasons = ApplySort(reasons, query.Sort);

        var totalCount = await reasons.CountAsync(cancellationToken);
        var items = await reasons.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<StockAdjustmentReason> ApplySort(IQueryable<StockAdjustmentReason> reasons, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return reasons.OrderBy(s => s.ReasonName);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "reasoncode" => descending ? reasons.OrderByDescending(s => s.ReasonCode) : reasons.OrderBy(s => s.ReasonCode),
            "updatedat" => descending ? reasons.OrderByDescending(s => s.UpdatedAt) : reasons.OrderBy(s => s.UpdatedAt),
            _ => descending ? reasons.OrderByDescending(s => s.ReasonName) : reasons.OrderBy(s => s.ReasonName),
        };
    }
}
