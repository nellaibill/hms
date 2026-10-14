using HMS.Modules.Pharmacy.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Domain;

/// <summary>
/// Append-only ledger row — one per Receive/Dispense action, snapshotting BalanceAfter at
/// commit time so the running balance's history is reconstructable without replaying every
/// mutation against PharmacyStockBalance. Never updated or soft-deleted once written: no
/// Update method here, and no IsDeleted query filter in PharmacyStockTransactionConfiguration
/// (unlike most entities in this codebase — see that configuration's own doc comment).
/// </summary>
internal class PharmacyStockTransaction : Entity
{
    public Guid StoreId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid ProductBatchId { get; private set; }
    public TransactionType TransactionType { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public Guid? PatientId { get; private set; }
    public Guid? AdmissionId { get; private set; }
    public string? Remarks { get; private set; }

    /// <summary>What one unit cost the pharmacy, and what it was sold for, as they stood when
    /// this row was written — snapshotted so margin and stock valuation for a past period do
    /// not shift when a batch's rates are later corrected. Null where not known (a receipt
    /// with no purchase rate recorded) or not applicable (UnitPrice on anything but a sale).</summary>
    public decimal? UnitCost { get; private set; }
    public decimal? UnitPrice { get; private set; }

    /// <summary>The document this movement was posted from — a goods receipt, a pharmacy bill,
    /// a transfer — and its printed number, so the ledger reads on its own. Null on rows
    /// written before documents existed, and on the old single-line receive/dispense.</summary>
    public StockSourceType? SourceType { get; private set; }
    public Guid? SourceId { get; private set; }
    public string? SourceNo { get; private set; }

    /// <summary>Set once, after the fact, by a successful best-effort billing attempt — see
    /// DispenseService.CreateAsync. Null means either this is a Receipt (never billed) or a
    /// Dispense whose billing attempt hasn't succeeded (not yet tried, or tried and failed;
    /// the two aren't distinguished here since nothing is retried automatically — the
    /// immediate create response's BillingFailed/BillingError carry that one-time detail).</summary>
    public Guid? InvoiceId { get; private set; }

    // Required by EF Core materialization.
    private PharmacyStockTransaction()
    {
    }

    private PharmacyStockTransaction(
        Guid id,
        Guid storeId,
        Guid productId,
        Guid productBatchId,
        TransactionType transactionType,
        decimal quantity,
        decimal balanceAfter,
        DateTime transactionDate,
        Guid? patientId,
        Guid? admissionId,
        string? remarks,
        decimal? unitCost,
        decimal? unitPrice,
        Guid? createdBy)
        : base(id, createdBy)
    {
        StoreId = storeId;
        ProductId = productId;
        ProductBatchId = productBatchId;
        TransactionType = transactionType;
        Quantity = quantity;
        BalanceAfter = balanceAfter;
        TransactionDate = transactionDate;
        PatientId = patientId;
        AdmissionId = admissionId;
        Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();
        UnitCost = unitCost;
        UnitPrice = unitPrice;
    }

    /// <summary>True for the movement types that add to a store's stock.</summary>
    public static bool IsInward(TransactionType type) => type is TransactionType.Receipt or TransactionType.OpeningStock
        or TransactionType.SaleReturn or TransactionType.TransferIn or TransactionType.AdjustmentIn;

    /// <summary>A movement posted from a document — see StockPoster, the only caller.</summary>
    public static PharmacyStockTransaction CreateFromDocument(
        TransactionType transactionType,
        Guid storeId,
        Guid productId,
        Guid productBatchId,
        decimal quantity,
        decimal balanceAfter,
        decimal? unitCost,
        decimal? unitPrice,
        StockSourceType? sourceType,
        Guid? sourceId,
        string? sourceNo,
        Guid? patientId,
        Guid? admissionId,
        string? remarks,
        Guid? createdBy)
    {
        Guard.AgainstNonPositive(quantity, nameof(quantity));

        return new PharmacyStockTransaction(
            Guid.CreateVersion7(), storeId, productId, productBatchId, transactionType, quantity, balanceAfter, DateTime.UtcNow,
            patientId, admissionId, remarks, unitCost, unitPrice, createdBy)
        {
            SourceType = sourceType,
            SourceId = sourceId,
            SourceNo = sourceNo,
        };
    }

    /// <param name="transactionType">Receipt, or OpeningStock for stock entered at go-live.</param>
    public static PharmacyStockTransaction CreateReceipt(
        Guid storeId,
        Guid productId,
        Guid productBatchId,
        decimal quantity,
        decimal balanceAfter,
        decimal? unitCost,
        string? remarks,
        Guid? createdBy,
        TransactionType transactionType = TransactionType.Receipt)
    {
        Guard.AgainstNonPositive(quantity, nameof(quantity));
        if (transactionType is not (TransactionType.Receipt or TransactionType.OpeningStock))
        {
            throw new ArgumentOutOfRangeException(nameof(transactionType), "A receipt is either Receipt or OpeningStock.");
        }

        return new PharmacyStockTransaction(
            Guid.CreateVersion7(),
            storeId,
            productId,
            productBatchId,
            transactionType,
            quantity,
            balanceAfter,
            DateTime.UtcNow,
            patientId: null,
            admissionId: null,
            remarks,
            unitCost,
            unitPrice: null,
            createdBy);
    }

    public static PharmacyStockTransaction CreateDispense(
        Guid storeId,
        Guid productId,
        Guid productBatchId,
        decimal quantity,
        decimal balanceAfter,
        Guid patientId,
        Guid? admissionId,
        decimal? unitCost,
        decimal unitPrice,
        string? remarks,
        Guid? createdBy)
    {
        Guard.AgainstNonPositive(quantity, nameof(quantity));

        return new PharmacyStockTransaction(
            Guid.CreateVersion7(),
            storeId,
            productId,
            productBatchId,
            TransactionType.Dispense,
            quantity,
            balanceAfter,
            DateTime.UtcNow,
            patientId,
            admissionId,
            remarks,
            unitCost,
            unitPrice,
            createdBy);
    }

    /// <summary>
    /// The one narrow, deliberate exception to "no Update method on PharmacyStockTransaction"
    /// (see the class doc comment): every stock/financial fact about the dispense itself
    /// (Quantity, BalanceAfter, TransactionType, ...) stays immutable, but which invoice ended
    /// up covering it is discovered strictly after the fact, only once billing has actually
    /// succeeded — see DispenseService.CreateAsync's two-phase dispense-then-bill flow. Guards
    /// against calling it on a Receipt (never billed) or twice on the same transaction (each
    /// dispense generates exactly one invoice, at most once).
    /// </summary>
    public void SetInvoiceId(Guid invoiceId, Guid? updatedBy)
    {
        if (TransactionType != TransactionType.Dispense)
        {
            throw new InvalidOperationException("Only a Dispense transaction can have an InvoiceId.");
        }

        if (InvoiceId is not null)
        {
            throw new InvalidOperationException("This transaction's InvoiceId has already been set.");
        }

        InvoiceId = invoiceId;
        MarkUpdated(updatedBy);
    }
}
