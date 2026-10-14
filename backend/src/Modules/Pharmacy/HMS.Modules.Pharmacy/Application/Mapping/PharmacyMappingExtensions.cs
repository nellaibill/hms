using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Modules.Products.Contracts;

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
        StoreId = transaction.StoreId,
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
        StoreId = transaction.StoreId,
        ProductId = transaction.ProductId,
        ProductName = productName,
        ProductBatchId = transaction.ProductBatchId,
        BatchNo = batchNo,
        PatientId = transaction.PatientId ?? Guid.Empty,
        PatientName = patientName,
        AdmissionId = transaction.AdmissionId,
        Quantity = transaction.Quantity,
        UnitPrice = transaction.UnitPrice,
        BalanceAfter = transaction.BalanceAfter,
        TransactionDate = transaction.TransactionDate,
        Remarks = transaction.Remarks,
        CreatedAt = transaction.CreatedAt,
        InvoiceId = transaction.InvoiceId,
        InvoiceNumber = invoiceNumber,
        BillingFailed = billingFailed,
        BillingError = billingError,
    };

    /// <summary>Product/batch are null when the Products lookup couldn't resolve them — the
    /// balance row is still returned, just without its display fields.</summary>
    public static StockBalanceResponse ToResponse(this PharmacyStockBalance balance, ProductResponse? product, ProductBatchResponse? batch) => new()
    {
        Id = balance.Id,
        StoreId = balance.StoreId,
        ProductId = balance.ProductId,
        ProductName = product?.ProductName ?? string.Empty,
        ProductBatchId = balance.ProductBatchId,
        BatchNo = batch?.BatchNo ?? string.Empty,
        ExpiryDate = batch?.ExpiryDate ?? default,
        BatchIsActive = batch?.IsActive ?? false,
        QuantityOnHand = balance.QuantityOnHand,
        CreatedAt = balance.CreatedAt,
        UpdatedAt = balance.UpdatedAt,
    };

    public static StockTransactionResponse ToLedgerResponse(this PharmacyStockTransaction transaction, string productName, string batchNo, string? patientName) => new()
    {
        Id = transaction.Id,
        StoreId = transaction.StoreId,
        ProductId = transaction.ProductId,
        ProductName = productName,
        ProductBatchId = transaction.ProductBatchId,
        BatchNo = batchNo,
        TransactionType = transaction.TransactionType,
        Quantity = transaction.Quantity,
        UnitPrice = transaction.UnitPrice,
        BalanceAfter = transaction.BalanceAfter,
        TransactionDate = transaction.TransactionDate,
        PatientId = transaction.PatientId,
        PatientName = patientName,
        AdmissionId = transaction.AdmissionId,
        Remarks = transaction.Remarks,
        SourceType = transaction.SourceType,
        SourceId = transaction.SourceId,
        SourceNo = transaction.SourceNo,
        CreatedAt = transaction.CreatedAt,
    };
}
