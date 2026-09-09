namespace HMS.Modules.IPD.Contracts;

public record CreateAdmissionAdvanceRequest
{
    public decimal Amount { get; init; }
    public PaymentMethod Method { get; init; }
    public string? ReferenceNumber { get; init; }
    public string? Remarks { get; init; }
}

public record AdmissionAdvanceResponse
{
    public Guid Id { get; init; }
    public Guid AdmissionId { get; init; }
    public decimal Amount { get; init; }
    public PaymentMethod Method { get; init; }
    public string? ReferenceNumber { get; init; }
    public string? Remarks { get; init; }
    public DateTime CreatedAt { get; init; }
}
