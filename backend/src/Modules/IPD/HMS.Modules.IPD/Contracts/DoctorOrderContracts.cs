namespace HMS.Modules.IPD.Contracts;

public record CreateDoctorOrderRequest
{
    public DoctorOrderType OrderType { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? Instructions { get; init; }
    public DateTime OrderedAt { get; init; }
}

public record CancelDoctorOrderRequest
{
    public string? Reason { get; init; }
}

public record DoctorOrderResponse
{
    public Guid Id { get; init; }
    public Guid AdmissionId { get; init; }
    public DoctorOrderType OrderType { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? Instructions { get; init; }
    public DateTime OrderedAt { get; init; }
    public Guid? OrderedByUserId { get; init; }
    public DoctorOrderStatus Status { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
    public DateTime CreatedAt { get; init; }
}
