namespace HMS.Modules.IPD.Contracts;

public record CreateMedicationAdministrationRequest
{
    public DateTime ScheduledTime { get; init; }
    public bool WasGiven { get; init; }
    public DateTime? AdministeredAt { get; init; }
    public string? Reason { get; init; }
    public string? Remarks { get; init; }
}

public record MedicationAdministrationResponse
{
    public Guid Id { get; init; }
    public Guid MedicationOrderId { get; init; }
    public DateTime ScheduledTime { get; init; }
    public bool WasGiven { get; init; }
    public DateTime? AdministeredAt { get; init; }
    public string? Reason { get; init; }
    public string? Remarks { get; init; }
    public Guid? RecordedByUserId { get; init; }
    public DateTime CreatedAt { get; init; }
}
