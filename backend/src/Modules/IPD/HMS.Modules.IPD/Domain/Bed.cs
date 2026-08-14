using HMS.Modules.IPD.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Domain;

/// <summary>
/// A physical inpatient bed inside a Ward. BedNumber is unique within its ward (not
/// globally) — see BedConfiguration's composite unique index. Status is normally driven by
/// the admission workflow (Occupied on admission, Available on discharge/transfer-out —
/// see AdmissionService), but can also be set directly here (e.g. Maintenance).
/// </summary>
internal class Bed : Entity
{
    public Guid WardId { get; private set; }
    public string BedNumber { get; private set; } = null!;
    public BedType BedType { get; private set; }
    public BedStatus Status { get; private set; }
    public bool IsActive { get; private set; } = true;
    public decimal DailyCharge { get; private set; }

    // Required by EF Core materialization.
    private Bed()
    {
    }

    private Bed(
        Guid id,
        Guid wardId,
        string bedNumber,
        BedType bedType,
        BedStatus status,
        bool isActive,
        decimal dailyCharge,
        Guid? createdBy)
        : base(id, createdBy)
    {
        WardId = wardId;
        BedNumber = bedNumber;
        BedType = bedType;
        Status = status;
        IsActive = isActive;
        DailyCharge = dailyCharge;
    }

    public static Bed Create(
        Guid wardId,
        string bedNumber,
        BedType bedType,
        BedStatus status,
        bool isActive,
        decimal dailyCharge,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(bedNumber, nameof(bedNumber));

        return new Bed(
            Guid.CreateVersion7(),
            wardId,
            bedNumber.Trim().ToUpperInvariant(),
            bedType,
            status,
            isActive,
            dailyCharge,
            createdBy);
    }

    public void Update(BedType bedType, BedStatus status, bool isActive, decimal dailyCharge, Guid? updatedBy)
    {
        BedType = bedType;
        Status = status;
        IsActive = isActive;
        DailyCharge = dailyCharge;
        MarkUpdated(updatedBy);
    }

    /// <summary>Driven by AdmissionService on admit/transfer/discharge — see docs/DecisionLog.md.</summary>
    public void SetStatus(BedStatus status, Guid? updatedBy)
    {
        Status = status;
        MarkUpdated(updatedBy);
    }
}
