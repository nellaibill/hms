using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Contracts;

public record CreateBedRequest
{
    public Guid WardId { get; init; }
    public string BedNumber { get; init; } = string.Empty;
    public string BedType { get; init; } = string.Empty;
    public BedStatus Status { get; init; } = BedStatus.Available;
    public bool IsActive { get; init; } = true;
    public decimal DailyCharge { get; init; }
}

// WardId/BedNumber are intentionally absent — natural-key fields, protected from change
// after creation. Use the bed-transfer workflow to move a patient between beds.
public record UpdateBedRequest
{
    public string BedType { get; init; } = string.Empty;
    public BedStatus Status { get; init; }
    public bool IsActive { get; init; } = true;
    public decimal DailyCharge { get; init; }
}

public record BedResponse
{
    public Guid Id { get; init; }
    public Guid WardId { get; init; }
    public string BedNumber { get; init; } = string.Empty;
    public string BedType { get; init; } = string.Empty;
    public BedStatus Status { get; init; }
    public bool IsActive { get; init; }
    public decimal DailyCharge { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class BedListQuery : PagedRequest
{
    public Guid? WardId { get; set; }
    public BedStatus? Status { get; set; }
    public bool? IsActive { get; set; }
}
