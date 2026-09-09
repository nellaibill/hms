using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Domain;

/// <summary>
/// A single recorded MAR (Medication Administration Record) event against a
/// MedicationOrder — one entry per actual administration check, not an auto-generated
/// schedule slot (see MedicationOrder's own doc comment on the deliberate no-auto-schedule
/// decision). Append-only, same convention as VitalsReading/ProgressNote/NursingNote: a
/// correction is a new entry, not an edit to clinical history. No navigation collection on
/// MedicationOrder itself, mirroring how Admission's own child entities are queried by
/// AdmissionId rather than loaded as a navigation collection.
/// </summary>
internal class MedicationAdministration : Entity
{
    public Guid MedicationOrderId { get; private set; }

    /// <summary>The due time this record is for.</summary>
    public DateTime ScheduledTime { get; private set; }

    public bool WasGiven { get; private set; }

    /// <summary>Only set when WasGiven — may differ slightly from ScheduledTime.</summary>
    public DateTime? AdministeredAt { get; private set; }

    /// <summary>Expected (soft, not DB-enforced) when !WasGiven — why the dose was withheld.</summary>
    public string? Reason { get; private set; }

    public string? Remarks { get; private set; }

    /// <summary>Opaque reference into identity.users — same no-FK convention as every other
    /// "recorded by" field across the IPD-expansion slices.</summary>
    public Guid? RecordedByUserId { get; private set; }

    // Required by EF Core materialization.
    private MedicationAdministration()
    {
    }

    private MedicationAdministration(
        Guid id,
        Guid medicationOrderId,
        DateTime scheduledTime,
        bool wasGiven,
        DateTime? administeredAt,
        string? reason,
        string? remarks,
        Guid? recordedByUserId,
        Guid? createdBy)
        : base(id, createdBy)
    {
        MedicationOrderId = medicationOrderId;
        ScheduledTime = scheduledTime;
        WasGiven = wasGiven;
        AdministeredAt = wasGiven ? administeredAt : null;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();
        RecordedByUserId = recordedByUserId;
    }

    public static MedicationAdministration Create(
        Guid medicationOrderId,
        DateTime scheduledTime,
        bool wasGiven,
        DateTime? administeredAt,
        string? reason,
        string? remarks,
        Guid? recordedByUserId,
        Guid? createdBy)
    {
        return new MedicationAdministration(
            Guid.CreateVersion7(),
            medicationOrderId,
            scheduledTime,
            wasGiven,
            administeredAt,
            reason,
            remarks,
            recordedByUserId,
            createdBy);
    }
}
