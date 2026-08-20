using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Domain;

/// <summary>
/// Current on-hand quantity for one (ProductId, ProductBatchId) pair — one row per
/// product/batch, mutated in place by Receive/Dispense. The authoritative running total;
/// PharmacyStockTransaction is the append-only ledger of how it got there.
/// ProductId/ProductBatchId reference the Products module's own aggregates — validated via
/// IProductService/IProductBatchService in Application (docs/DeveloperHandbook.md's
/// module-boundary rule: depend only on another module's public Contracts/Application seam,
/// never its Domain/Infrastructure) — so there is no local FK, same as Admission's
/// PatientId/DepartmentId/ConsultantId in the IPD module.
/// </summary>
internal class PharmacyStockBalance : Entity
{
    public Guid ProductId { get; private set; }
    public Guid ProductBatchId { get; private set; }
    public decimal QuantityOnHand { get; private set; }

    // Required by EF Core materialization.
    private PharmacyStockBalance()
    {
    }

    private PharmacyStockBalance(Guid id, Guid productId, Guid productBatchId, Guid? createdBy)
        : base(id, createdBy)
    {
        ProductId = productId;
        ProductBatchId = productBatchId;
        QuantityOnHand = 0m;
    }

    public static PharmacyStockBalance Create(Guid productId, Guid productBatchId, Guid? createdBy)
        => new(Guid.CreateVersion7(), productId, productBatchId, createdBy);

    public void Receive(decimal quantity, Guid? updatedBy)
    {
        Guard.AgainstNonPositive(quantity, nameof(quantity));

        QuantityOnHand += quantity;
        MarkUpdated(updatedBy);
    }

    /// <summary>
    /// Defense-in-depth: DispenseService is expected to have already validated
    /// <c>quantity &lt;= QuantityOnHand</c> against a freshly-read balance (and to retry on a
    /// concurrency conflict — see DispenseService.CreateAsync) before ever calling this, so
    /// this guard should never actually fire in normal operation. It exists so the
    /// "never negative" invariant can't be violated even by a future caller that skips that
    /// check.
    /// </summary>
    public void Dispense(decimal quantity, Guid? updatedBy)
    {
        Guard.AgainstNonPositive(quantity, nameof(quantity));

        if (quantity > QuantityOnHand)
        {
            throw new InvalidOperationException(
                $"Cannot dispense {quantity} of product/batch — only {QuantityOnHand} on hand.");
        }

        QuantityOnHand -= quantity;
        MarkUpdated(updatedBy);
    }
}
