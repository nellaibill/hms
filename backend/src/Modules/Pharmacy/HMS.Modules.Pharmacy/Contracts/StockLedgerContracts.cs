using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Contracts;

/// <summary>
/// Denormalizes ProductName/BatchNo/PatientName (resolved once per request in
/// StockLedgerService via IProductService/IProductBatchService/IPatientService, not stored)
/// — same pattern as IPD's AdmissionResponse. One row per PharmacyStockTransaction, covering
/// both Receipt and Dispense entries.
/// </summary>
public record StockTransactionResponse
{
    public Guid Id { get; init; }
    public Guid StoreId { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public Guid ProductBatchId { get; init; }
    public string BatchNo { get; init; } = string.Empty;
    public TransactionType TransactionType { get; init; }
    public decimal Quantity { get; init; }

    /// <summary>What one unit was sold for, on a dispense. Null otherwise.</summary>
    public decimal? UnitPrice { get; init; }

    public decimal BalanceAfter { get; init; }
    public DateTime TransactionDate { get; init; }
    public Guid? PatientId { get; init; }
    public string? PatientName { get; init; }
    public Guid? AdmissionId { get; init; }
    public string? Remarks { get; init; }

    /// <summary>The document this movement was posted from, when there is one.</summary>
    public StockSourceType? SourceType { get; init; }
    public Guid? SourceId { get; init; }
    public string? SourceNo { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class StockLedgerListQuery : PagedRequest
{
    public Guid? ProductId { get; set; }
    public Guid? ProductBatchId { get; set; }

    /// <summary>Limits the list to one store. Omitted, every store is included.</summary>
    public Guid? StoreId { get; set; }
    public TransactionType? TransactionType { get; set; }
    public Guid? PatientId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
