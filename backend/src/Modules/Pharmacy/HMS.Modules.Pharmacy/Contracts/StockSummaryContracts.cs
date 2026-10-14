using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Contracts;

/// <summary>
/// One row per product: its on-hand stock across every batch, split by whether it can
/// actually be dispensed. StockBalanceResponse stays the per-batch view; this is what a
/// low-stock decision has to be made on — a product held as three small batches is not
/// "low" just because each batch alone is under the reorder level.
/// </summary>
public record StockSummaryResponse
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string? GenericName { get; init; }
    public decimal ReorderLevel { get; init; }

    /// <summary>On hand in active, unexpired batches — the only stock a dispense can draw on.</summary>
    public decimal AvailableQuantity { get; init; }

    /// <summary>The part of <see cref="AvailableQuantity"/> expiring within the expiring-soon window.</summary>
    public decimal ExpiringSoonQuantity { get; init; }

    /// <summary>On hand in batches past their expiry date — awaiting return or write-off.</summary>
    public decimal ExpiredQuantity { get; init; }

    /// <summary>On hand in unexpired batches that have been marked inactive.</summary>
    public decimal OnHoldQuantity { get; init; }

    /// <summary>Number of batches with any stock on hand, dispensable or not.</summary>
    public int BatchCount { get; init; }

    /// <summary>Earliest expiry among the batches counted in <see cref="AvailableQuantity"/>.</summary>
    public DateOnly? NearestExpiry { get; init; }

    public StockStatus Status { get; init; }
}

public class StockSummaryListQuery : PagedRequest
{
    public StockSummaryFilter? Filter { get; set; }
}

/// <summary>
/// A batch a dispense can draw on right now — active, unexpired, with stock on hand.
/// Returned earliest-expiry-first so the first row is the one to dispense (FEFO).
/// </summary>
public record AvailableBatchResponse
{
    public Guid ProductBatchId { get; init; }
    public string BatchNo { get; init; } = string.Empty;
    public DateOnly ExpiryDate { get; init; }
    public decimal QuantityOnHand { get; init; }
    public bool IsExpiringSoon { get; init; }
}
