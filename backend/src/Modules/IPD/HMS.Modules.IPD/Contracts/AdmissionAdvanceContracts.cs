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

    /// <summary>The admission's linked visit's IP No — set only on the record-advance response
    /// once it has one, which a paid advance is what issues (ADR-104). Null on every read.</summary>
    public string? IpNumber { get; init; }
}
