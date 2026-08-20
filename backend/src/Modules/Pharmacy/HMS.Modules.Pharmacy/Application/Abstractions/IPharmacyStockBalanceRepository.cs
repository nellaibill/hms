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
internal interface IPharmacyStockBalanceRepository
{
    Task AddAsync(PharmacyStockBalance balance, CancellationToken cancellationToken);

    Task<PharmacyStockBalance?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PharmacyStockBalance?> GetByProductAndBatchAsync(Guid productId, Guid productBatchId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<PharmacyStockBalance> Items, int TotalCount)> GetPagedAsync(StockBalanceListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
