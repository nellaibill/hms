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
    public Guid ProductId { get; private set; }
    public Guid ProductBatchId { get; private set; }
    public TransactionType TransactionType { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public Guid? PatientId { get; private set; }
    public Guid? AdmissionId { get; private set; }
    public string? Remarks { get; private set; }

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
        Guid productId,
        Guid productBatchId,
        TransactionType transactionType,
        decimal quantity,
        decimal balanceAfter,
        DateTime transactionDate,
        Guid? patientId,
        Guid? admissionId,
        string? remarks,
        Guid? createdBy)
        : base(id, createdBy)
    {
        ProductId = productId;
        ProductBatchId = productBatchId;
        TransactionType = transactionType;
        Quantity = quantity;
        BalanceAfter = balanceAfter;
        TransactionDate = transactionDate;
        PatientId = patientId;
        AdmissionId = admissionId;
        Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();
    }

    public static PharmacyStockTransaction CreateReceipt(
        Guid productId,
        Guid productBatchId,
        decimal quantity,
        decimal balanceAfter,
        string? remarks,
        Guid? createdBy)
    {
        Guard.AgainstNonPositive(quantity, nameof(quantity));

        return new PharmacyStockTransaction(
            Guid.CreateVersion7(),
            productId,
            productBatchId,
            TransactionType.Receipt,
            quantity,
            balanceAfter,
            DateTime.UtcNow,
            patientId: null,
            admissionId: null,
            remarks,
            createdBy);
    }

    public static PharmacyStockTransaction CreateDispense(
        Guid productId,
        Guid productBatchId,
        decimal quantity,
        decimal balanceAfter,
        Guid patientId,
        Guid? admissionId,
        string? remarks,
        Guid? createdBy)
    {
        Guard.AgainstNonPositive(quantity, nameof(quantity));

        return new PharmacyStockTransaction(
            Guid.CreateVersion7(),
            productId,
            productBatchId,
            TransactionType.Dispense,
            quantity,
            balanceAfter,
            DateTime.UtcNow,
            patientId,
            admissionId,
            remarks,
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
