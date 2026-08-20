using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;

namespace HMS.Modules.Pharmacy.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule in docs/DeveloperHandbook.md — Application never references EF Core types.
/// No SaveChangesAsync — ledger rows are always written inside StockReceiptService/
/// DispenseService alongside a PharmacyStockBalance mutation, persisted via
/// IPharmacyStockBalanceRepository.SaveChangesAsync on the same DbContext (mirrors IPD's
/// IAdmissionBedStayRepository).
/// </summary>
internal interface IPharmacyStockTransactionRepository
{
    Task AddAsync(PharmacyStockTransaction transaction, CancellationToken cancellationToken);

    Task<PharmacyStockTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Shared paged/filtered query backing StockLedgerService directly, and reused by
    /// StockReceiptService/DispenseService (which each build a StockLedgerListQuery scoped to
    /// their own TransactionType before calling this) so the filtering logic lives in one
    /// place instead of three.
    /// </summary>
    Task<(IReadOnlyList<PharmacyStockTransaction> Items, int TotalCount)> GetPagedAsync(StockLedgerListQuery query, CancellationToken cancellationToken);
}
