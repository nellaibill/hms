using HMS.Modules.IPD.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Domain;

/// <summary>
/// A single advance/deposit payment collected from the patient's family during an inpatient
/// stay (typically at admission, sometimes topped up later) — posted against an Admission.
/// Append-only, same convention as every log-shaped IPD entity (no update/delete). Deliberately
/// NOT reconciled against HMS.Modules.Billing's Invoice/Payment model: that model is
/// all-or-nothing per invoice with no partial-payment or credit/refund concept, so this ledger
/// only tracks collection and lets staff see the computed balance — see ADR-067.
/// </summary>
internal class AdmissionAdvance : Entity
{
    public Guid AdmissionId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public string? Remarks { get; private set; }

    // Required by EF Core materialization.
    private AdmissionAdvance()
    {
    }

    private AdmissionAdvance(
        Guid id,
        Guid admissionId,
        decimal amount,
        PaymentMethod method,
        string? referenceNumber,
        string? remarks,
        Guid? createdBy)
        : base(id, createdBy)
    {
        AdmissionId = admissionId;
        Amount = amount;
        Method = method;
        ReferenceNumber = string.IsNullOrWhiteSpace(referenceNumber) ? null : referenceNumber.Trim();
        Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();
    }

    public static AdmissionAdvance Create(
        Guid admissionId,
        decimal amount,
        PaymentMethod method,
        string? referenceNumber,
        string? remarks,
        Guid? createdBy)
    {
        Guard.AgainstNonPositive(amount, nameof(amount));

        return new AdmissionAdvance(
            Guid.CreateVersion7(),
            admissionId,
            amount,
            method,
            referenceNumber,
            remarks,
            createdBy);
    }
}
