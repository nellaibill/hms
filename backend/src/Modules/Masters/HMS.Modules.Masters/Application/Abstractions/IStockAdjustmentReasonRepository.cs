using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

internal interface IStockAdjustmentReasonRepository
{
    Task AddAsync(StockAdjustmentReason reason, CancellationToken cancellationToken);

    Task<StockAdjustmentReason?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string reasonCode, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<StockAdjustmentReason> Items, int TotalCount)> GetPagedAsync(StockAdjustmentReasonListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
