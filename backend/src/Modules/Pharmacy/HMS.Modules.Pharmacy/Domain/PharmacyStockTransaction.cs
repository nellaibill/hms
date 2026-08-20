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
}
