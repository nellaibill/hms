using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Contracts;

/// <summary>
/// Denormalizes ProductName/BatchNo/ExpiryDate (resolved once per request in
/// StockBalanceService via IProductService/IProductBatchService, not stored) — same pattern
/// as IPD's AdmissionResponse.
/// </summary>
public record StockBalanceResponse
{
    public Guid Id { get; init; }
    public Guid StoreId { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public Guid ProductBatchId { get; init; }
    public string BatchNo { get; init; } = string.Empty;
    public DateOnly ExpiryDate { get; init; }

    /// <summary>False when the batch has been marked inactive in Products — its stock is
    /// still counted here but can't be dispensed.</summary>
    public bool BatchIsActive { get; init; }

    public decimal QuantityOnHand { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class StockBalanceListQuery : PagedRequest
{
    public Guid? ProductId { get; set; }

    /// <summary>Limits the list to one store. Omitted, every store is included.</summary>
    public Guid? StoreId { get; set; }

    /// <summary>When true, leaves out batches whose balance has run down to zero — every
    /// batch ever received keeps its balance row, so a long-stocked product otherwise lists
    /// mostly empty ones.</summary>
    public bool? InStockOnly { get; set; }
}
