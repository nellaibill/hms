using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;

namespace HMS.Modules.Pharmacy.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule in docs/DeveloperHandbook.md — Application never references EF Core types.
/// The "primary" repository for stock mutations: StockReceiptService/DispenseService call
/// SaveChangesAsync through this one (both repositories share the same PharmacyDbContext
/// instance, so a single SaveChangesAsync call commits changes tracked via either) — mirrors
/// IPD's IAdmissionRepository/IAdmissionBedStayRepository split.
/// </summary>
/// <summary>One batch's on-hand quantity, read untracked for the stock summary — nothing
/// that aggregates across products needs a full, change-tracked PharmacyStockBalance.</summary>
internal sealed record StockBalanceLine(Guid ProductId, Guid ProductBatchId, decimal QuantityOnHand);

internal interface IPharmacyStockBalanceRepository
{
    Task AddAsync(PharmacyStockBalance balance, CancellationToken cancellationToken);

    Task<PharmacyStockBalance?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PharmacyStockBalance?> GetByProductAndBatchAsync(Guid productId, Guid productBatchId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<PharmacyStockBalance> Items, int TotalCount)> GetPagedAsync(StockBalanceListQuery query, CancellationToken cancellationToken);

    /// <summary>Every balance row (zero-quantity ones included — a zero row still says the
    /// product has been stocked before) for the given products, or for all products when
    /// <paramref name="productIds"/> is null.</summary>
    Task<IReadOnlyList<StockBalanceLine>> GetLinesAsync(IReadOnlyCollection<Guid>? productIds, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
