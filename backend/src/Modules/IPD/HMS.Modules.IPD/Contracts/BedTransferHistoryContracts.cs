namespace HMS.Modules.IPD.Contracts;

/// <summary>
/// Denormalizes old/new ward name + bed number so the frontend doesn't need extra
/// round-trips per row — resolved once per request in AdmissionService, not stored.
/// </summary>
public record BedTransferHistoryResponse
{
    public Guid Id { get; init; }
    public Guid AdmissionId { get; init; }

    public Guid OldWardId { get; init; }
    public string OldWardName { get; init; } = string.Empty;
    public Guid OldBedId { get; init; }
    public string OldBedNumber { get; init; } = string.Empty;

    public Guid NewWardId { get; init; }
    public string NewWardName { get; init; } = string.Empty;
    public Guid NewBedId { get; init; }
    public string NewBedNumber { get; init; } = string.Empty;

    public string? TransferReason { get; init; }
    public DateTime TransferredAt { get; init; }
}
