using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Domain;

/// <summary>
/// One record of an Admission's ward/bed move, written alongside the mutation in
/// AdmissionService.TransferBedAsync — TransferBed() on Admission itself only overwrites
/// the current WardId/BedId, so this is the only place prior moves are recoverable from.
/// </summary>
internal class BedTransferHistory : Entity
{
    public Guid AdmissionId { get; private set; }
    public Guid OldWardId { get; private set; }
    public Guid OldBedId { get; private set; }
    public Guid NewWardId { get; private set; }
    public Guid NewBedId { get; private set; }
    public string? TransferReason { get; private set; }

    // Required by EF Core materialization.
    private BedTransferHistory()
    {
    }

    private BedTransferHistory(
        Guid id,
        Guid admissionId,
        Guid oldWardId,
        Guid oldBedId,
        Guid newWardId,
        Guid newBedId,
        string? transferReason,
        Guid? createdBy)
        : base(id, createdBy)
    {
        AdmissionId = admissionId;
        OldWardId = oldWardId;
        OldBedId = oldBedId;
        NewWardId = newWardId;
        NewBedId = newBedId;
        TransferReason = transferReason;
    }

    public static BedTransferHistory Create(
        Guid admissionId,
        Guid oldWardId,
        Guid oldBedId,
        Guid newWardId,
        Guid newBedId,
        string? transferReason,
        Guid? createdBy)
    {
        return new BedTransferHistory(
            Guid.CreateVersion7(),
            admissionId,
            oldWardId,
            oldBedId,
            newWardId,
            newBedId,
            string.IsNullOrWhiteSpace(transferReason) ? null : transferReason.Trim(),
            createdBy);
    }
}
