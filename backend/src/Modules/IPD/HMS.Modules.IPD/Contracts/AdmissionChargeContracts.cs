namespace HMS.Modules.IPD.Contracts;

public record CreateAdmissionChargeRequest
{
    public ChargeType ChargeType { get; init; }
    public decimal Amount { get; init; }
    public string? Remarks { get; init; }
}

public record AdmissionChargeResponse
{
    public Guid Id { get; init; }
    public Guid AdmissionId { get; init; }
    public ChargeType ChargeType { get; init; }
    public decimal Amount { get; init; }
    public string? Remarks { get; init; }
    public DateTime CreatedAt { get; init; }
}
