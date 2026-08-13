namespace HMS.Modules.IPD.Contracts;

/// <summary>Denormalizes bed number + ward name — resolved once per request in AdmissionService.</summary>
public record AdmissionBedStayResponse
{
    public Guid Id { get; init; }
    public Guid AdmissionId { get; init; }
    public Guid BedId { get; init; }
    public string BedNumber { get; init; } = string.Empty;
    public Guid WardId { get; init; }
    public string WardName { get; init; } = string.Empty;
    public DateTime FromDateTime { get; init; }
    public DateTime? ToDateTime { get; init; }
    public decimal DailyCharge { get; init; }
}
