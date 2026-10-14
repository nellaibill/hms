using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;

namespace HMS.Modules.Pharmacy.Application.Abstractions;

/// <summary>Where a product was last bought, and for how much a pack.</summary>
internal sealed record LastPurchase(Guid ProductId, Guid SupplierId, string SupplierName, decimal PurchaseRate, DateOnly ReceivedDate);

/// <summary>One supplier's totals across every goods receipt, return and payment.</summary>
internal sealed record SupplierTotals(Guid SupplierId, string SupplierName, decimal Purchases, decimal Returns, decimal Payments, DateOnly? LastPurchaseDate);

/// <summary>A dated entry on a supplier's account, before running balances are worked out.</summary>
internal sealed record SupplierEntry(DateOnly Date, DateTime CreatedAt, SupplierEntryType Type, Guid DocumentId, string DocumentNo, string? Reference, decimal Amount);

/// <summary>Purchase orders, purchase returns and supplier payments. Saved through IPharmacyUnitOfWork.</summary>
internal interface IPharmacyProcurementRepository
{
    Task AddOrderAsync(PharmacyPurchaseOrder order, CancellationToken cancellationToken);

    /// <summary>The order with its lines, tracked.</summary>
    Task<PharmacyPurchaseOrder?> GetOrderAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<(PharmacyPurchaseOrder Order, int LineCount, decimal Amount)> Items, int TotalCount)> GetOrdersPagedAsync(PurchaseOrderListQuery query, CancellationToken cancellationToken);

    /// <summary>Packs still to come on open orders, per product.</summary>
    Task<IReadOnlyDictionary<Guid, decimal>> GetOnOrderQuantitiesAsync(Guid? storeId, CancellationToken cancellationToken);

    /// <summary>The most recent goods receipt line for each of the given products.</summary>
    Task<IReadOnlyDictionary<Guid, LastPurchase>> GetLastPurchasesAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken);

    Task AddReturnAsync(PharmacyPurchaseReturn purchaseReturn, CancellationToken cancellationToken);

    Task<PharmacyPurchaseReturn?> GetReturnAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<(PharmacyPurchaseReturn Return, int LineCount)> Items, int TotalCount)> GetReturnsPagedAsync(PurchaseReturnListQuery query, CancellationToken cancellationToken);

    Task AddPaymentAsync(PharmacySupplierPayment payment, CancellationToken cancellationToken);

    Task<IReadOnlyList<SupplierTotals>> GetSupplierTotalsAsync(CancellationToken cancellationToken);

    /// <summary>Every purchase, return and payment on one supplier's account, oldest first.</summary>
    Task<IReadOnlyList<SupplierEntry>> GetSupplierEntriesAsync(Guid supplierId, CancellationToken cancellationToken);
}
