namespace HMS.Modules.IPD.Contracts;

public record CreateDoctorOrderRequest
{
    public DoctorOrderType OrderType { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? Instructions { get; init; }
    public DateTime OrderedAt { get; init; }

    /// <summary>Optional reference to a priced Masters catalog item (DiagnosticService for
    /// Radiology, DiagnosticTest for Procedure, ConsultationType for Consultation) — when set,
    /// DoctorOrderService auto-posts an AdmissionCharge for the resolved price. Left null for
    /// Diet/Nursing/Blood/Referral (no priced catalog exists) or for any free-text-only order
    /// where the doctor didn't pick a catalog item. See ADR-065.</summary>
    public Guid? CatalogItemId { get; init; }
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
    public Guid? CatalogItemId { get; init; }
    public DateTime OrderedAt { get; init; }
    public Guid? OrderedByUserId { get; init; }
    public DoctorOrderStatus Status { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
    public DateTime CreatedAt { get; init; }
}
