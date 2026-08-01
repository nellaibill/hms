using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreateStockAdjustmentReasonRequest
{
    public string ReasonCode { get; init; } = string.Empty;
    public string ReasonName { get; init; } = string.Empty;
    public bool AffectsValuation { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateStockAdjustmentReasonRequest
{
    public string ReasonName { get; init; } = string.Empty;
    public bool AffectsValuation { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

public record StockAdjustmentReasonResponse
{
    public Guid Id { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
    public string ReasonName { get; init; } = string.Empty;
    public bool AffectsValuation { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class StockAdjustmentReasonListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
