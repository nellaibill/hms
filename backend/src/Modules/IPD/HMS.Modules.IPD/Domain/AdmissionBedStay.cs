using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Domain;

/// <summary>
/// One continuous occupancy period of an Admission in a single Bed, with the DailyCharge
/// that applied during that period — written on admit/transfer, closed on transfer/discharge.
/// Distinct from BedTransferHistory (an audit log of transfer events): this is the billing
/// source of truth DischargeAsync reads to compute per-day highest-tariff bed charges.
/// </summary>
internal class AdmissionBedStay : Entity
{
    public Guid AdmissionId { get; private set; }
    public Guid BedId { get; private set; }
    public DateTime FromDateTime { get; private set; }
    public DateTime? ToDateTime { get; private set; }
    public decimal DailyCharge { get; private set; }

    // Required by EF Core materialization.
    private AdmissionBedStay()
    {
    }

    private AdmissionBedStay(Guid id, Guid admissionId, Guid bedId, DateTime fromDateTime, decimal dailyCharge, Guid? createdBy)
        : base(id, createdBy)
    {
        AdmissionId = admissionId;
        BedId = bedId;
        FromDateTime = fromDateTime;
        DailyCharge = dailyCharge;
    }

    public static AdmissionBedStay Create(Guid admissionId, Guid bedId, DateTime fromDateTime, decimal dailyCharge, Guid? createdBy)
        => new(Guid.CreateVersion7(), admissionId, bedId, fromDateTime, dailyCharge, createdBy);

    public void Close(DateTime toDateTime, Guid? updatedBy)
    {
        ToDateTime = toDateTime;
        MarkUpdated(updatedBy);
    }
}
