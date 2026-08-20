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
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public Guid ProductBatchId { get; init; }
    public string BatchNo { get; init; } = string.Empty;
    public DateOnly ExpiryDate { get; init; }
    public decimal QuantityOnHand { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class StockBalanceListQuery : PagedRequest
{
    public Guid? ProductId { get; set; }
}
