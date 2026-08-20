using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Contracts;

public record CreateStockReceiptRequest
{
    public Guid ProductId { get; init; }
    public Guid ProductBatchId { get; init; }
    public decimal Quantity { get; init; }
    public string? Remarks { get; init; }
}

/// <summary>
/// Denormalizes ProductName/BatchNo (resolved once per request in StockReceiptService via
/// IProductService/IProductBatchService, not stored) so list/detail consumers don't need an
/// extra round-trip per row — same pattern as IPD's AdmissionResponse.
/// </summary>
public record StockReceiptResponse
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public Guid ProductBatchId { get; init; }
    public string BatchNo { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal BalanceAfter { get; init; }
    public DateTime TransactionDate { get; init; }
    public string? Remarks { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class StockReceiptListQuery : PagedRequest
{
    public Guid? ProductId { get; set; }
}
