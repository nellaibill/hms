using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;

namespace HMS.Modules.Pharmacy.Application.Abstractions;

/// <summary>No SaveChangesAsync: a goods receipt is committed together with the stock it
/// moves, through IPharmacyUnitOfWork.</summary>
internal interface IPharmacyGoodsReceiptRepository
{
    Task AddAsync(PharmacyGoodsReceipt receipt, CancellationToken cancellationToken);

    /// <summary>The receipt with its lines, tracked.</summary>
    Task<PharmacyGoodsReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsForSupplierInvoiceAsync(Guid supplierId, string supplierInvoiceNo, CancellationToken cancellationToken);

    /// <summary>Headers only (no lines), newest first, with each receipt's line count.</summary>
    Task<(IReadOnlyList<(PharmacyGoodsReceipt Receipt, int LineCount)> Items, int TotalCount)> GetPagedAsync(GoodsReceiptListQuery query, CancellationToken cancellationToken);

    /// <summary>The receipt lines a batch came in on, newest first, tracked — what a purchase
    /// return is recorded against.</summary>
    Task<IReadOnlyList<PharmacyGoodsReceiptLine>> GetLinesForBatchAsync(Guid productBatchId, CancellationToken cancellationToken);
}
