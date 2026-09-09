namespace HMS.Modules.IPD.Contracts;

public record CreateMedicationOrderRequest
{
    public string DrugName { get; init; } = string.Empty;
    public string Dose { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public string Frequency { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Instructions { get; init; }
    public DateTime OrderedAt { get; init; }
}

public record DiscontinueMedicationOrderRequest
{
    public string? Reason { get; init; }
}

public record MedicationOrderResponse
{
    public Guid Id { get; init; }
    public Guid AdmissionId { get; init; }
    public string DrugName { get; init; } = string.Empty;
    public string Dose { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public string Frequency { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Instructions { get; init; }
    public DateTime OrderedAt { get; init; }
    public Guid? OrderedByUserId { get; init; }
    public MedicationOrderStatus Status { get; init; }
    public DateTime? DiscontinuedAt { get; init; }
    public string? DiscontinuedReason { get; init; }
    public DateTime CreatedAt { get; init; }
}
