using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;

namespace HMS.Modules.Pharmacy.Application.Mapping;

/// <summary>
/// Manual entity-to-Contracts-DTO mapping (no AutoMapper/Mapster — docs/DeveloperHandbook.md
/// §19's "avoid a mapping library for one or two DTOs" guidance). Unlike most modules'
/// mapping extensions, these all take extra denormalized display fields (product name, batch
/// number, patient name) as parameters, because that data lives in other modules
/// (Products/Patients) and is resolved by the caller via their public service seams — see
/// each *Service's BuildResponseAsync helper.
/// </summary>
internal static class PharmacyMappingExtensions
{
    public static StockReceiptResponse ToReceiptResponse(this PharmacyStockTransaction transaction, string productName, string batchNo) => new()
    {
        Id = transaction.Id,
        ProductId = transaction.ProductId,
        ProductName = productName,
        ProductBatchId = transaction.ProductBatchId,
        BatchNo = batchNo,
        Quantity = transaction.Quantity,
        BalanceAfter = transaction.BalanceAfter,
        TransactionDate = transaction.TransactionDate,
        Remarks = transaction.Remarks,
        CreatedAt = transaction.CreatedAt,
    };

    public static DispenseResponse ToDispenseResponse(
        this PharmacyStockTransaction transaction,
        string productName,
        string batchNo,
        string patientName,
        string? invoiceNumber = null,
        bool billingFailed = false,
        string? billingError = null) => new()
    {
        Id = transaction.Id,
        ProductId = transaction.ProductId,
        ProductName = productName,
        ProductBatchId = transaction.ProductBatchId,
        BatchNo = batchNo,
        PatientId = transaction.PatientId ?? Guid.Empty,
        PatientName = patientName,
        AdmissionId = transaction.AdmissionId,
        Quantity = transaction.Quantity,
        BalanceAfter = transaction.BalanceAfter,
        TransactionDate = transaction.TransactionDate,
        Remarks = transaction.Remarks,
        CreatedAt = transaction.CreatedAt,
        InvoiceId = transaction.InvoiceId,
        InvoiceNumber = invoiceNumber,
        BillingFailed = billingFailed,
        BillingError = billingError,
    };

    public static StockBalanceResponse ToResponse(this PharmacyStockBalance balance, string productName, string batchNo, DateOnly expiryDate) => new()
    {
        Id = balance.Id,
        ProductId = balance.ProductId,
        ProductName = productName,
        ProductBatchId = balance.ProductBatchId,
        BatchNo = batchNo,
        ExpiryDate = expiryDate,
        QuantityOnHand = balance.QuantityOnHand,
        CreatedAt = balance.CreatedAt,
        UpdatedAt = balance.UpdatedAt,
    };

    public static StockTransactionResponse ToLedgerResponse(this PharmacyStockTransaction transaction, string productName, string batchNo, string? patientName) => new()
    {
        Id = transaction.Id,
        ProductId = transaction.ProductId,
        ProductName = productName,
        ProductBatchId = transaction.ProductBatchId,
        BatchNo = batchNo,
        TransactionType = transaction.TransactionType,
        Quantity = transaction.Quantity,
        BalanceAfter = transaction.BalanceAfter,
        TransactionDate = transaction.TransactionDate,
        PatientId = transaction.PatientId,
        PatientName = patientName,
        AdmissionId = transaction.AdmissionId,
        Remarks = transaction.Remarks,
        CreatedAt = transaction.CreatedAt,
    };
}
