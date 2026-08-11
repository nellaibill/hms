using HMS.Modules.IPD.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Domain;

/// <summary>
/// A single inpatient charge line item (admission/bed/nursing) posted against an
/// Admission. This is IPD's own minimal ledger — the Billing module has no
/// implementation yet to post charges into (it is currently an empty placeholder;
/// see docs/DecisionLog.md), so this table is the seam Billing will eventually read
/// from/replace, mirroring how Consultant/Department were free text on Patients before
/// Masters existed.
/// </summary>
internal class AdmissionCharge : Entity
{
    public Guid AdmissionId { get; private set; }
    public ChargeType ChargeType { get; private set; }
    public decimal Amount { get; private set; }
    public string? Remarks { get; private set; }

    // Required by EF Core materialization.
    private AdmissionCharge()
    {
    }

    private AdmissionCharge(
        Guid id,
        Guid admissionId,
        ChargeType chargeType,
        decimal amount,
        string? remarks,
        Guid? createdBy)
        : base(id, createdBy)
    {
        AdmissionId = admissionId;
        ChargeType = chargeType;
        Amount = amount;
        Remarks = remarks;
    }

    public static AdmissionCharge Create(
        Guid admissionId,
        ChargeType chargeType,
        decimal amount,
        string? remarks,
        Guid? createdBy)
    {
        return new AdmissionCharge(
            Guid.CreateVersion7(),
            admissionId,
            chargeType,
            amount,
            string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim(),
            createdBy);
    }
}
