using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Domain;

/// <summary>
/// A single timestamped set of vital-sign observations recorded against an Admission.
/// Append-only — mirrors AdmissionCharge's own precedent (no Update/Delete): a correction is
/// a new reading, not an edit to clinical history. No navigation collection on Admission
/// itself, same convention as AdmissionCharge/BedTransferHistory.
/// </summary>
internal class VitalsReading : Entity
{
    public Guid AdmissionId { get; private set; }

    /// <summary>When the reading was taken — distinct from CreatedAt, since a nurse may log
    /// it slightly after the fact.</summary>
    public DateTime RecordedAt { get; private set; }

    public decimal? TemperatureF { get; private set; }
    public int? PulseRate { get; private set; }
    public int? RespiratoryRate { get; private set; }
    public int? BloodPressureSystolic { get; private set; }
    public int? BloodPressureDiastolic { get; private set; }
    public int? SpO2Percent { get; private set; }
    public decimal? WeightKg { get; private set; }
    public decimal? HeightCm { get; private set; }
    public int? PainScore { get; private set; }
    public decimal? BloodGlucoseMgDl { get; private set; }

    /// <summary>Opaque reference into identity.users — same no-FK convention as Discharge
    /// Summary's sign-off fields.</summary>
    public Guid? RecordedByUserId { get; private set; }

    public string? Notes { get; private set; }

    // Required by EF Core materialization.
    private VitalsReading()
    {
    }

    private VitalsReading(
        Guid id,
        Guid admissionId,
        DateTime recordedAt,
        decimal? temperatureF,
        int? pulseRate,
        int? respiratoryRate,
        int? bloodPressureSystolic,
        int? bloodPressureDiastolic,
        int? spO2Percent,
        decimal? weightKg,
        decimal? heightCm,
        int? painScore,
        decimal? bloodGlucoseMgDl,
        Guid? recordedByUserId,
        string? notes,
        Guid? createdBy)
        : base(id, createdBy)
    {
        AdmissionId = admissionId;
        RecordedAt = recordedAt;
        TemperatureF = temperatureF;
        PulseRate = pulseRate;
        RespiratoryRate = respiratoryRate;
        BloodPressureSystolic = bloodPressureSystolic;
        BloodPressureDiastolic = bloodPressureDiastolic;
        SpO2Percent = spO2Percent;
        WeightKg = weightKg;
        HeightCm = heightCm;
        PainScore = painScore;
        BloodGlucoseMgDl = bloodGlucoseMgDl;
        RecordedByUserId = recordedByUserId;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public static VitalsReading Create(
        Guid admissionId,
        DateTime recordedAt,
        decimal? temperatureF,
        int? pulseRate,
        int? respiratoryRate,
        int? bloodPressureSystolic,
        int? bloodPressureDiastolic,
        int? spO2Percent,
        decimal? weightKg,
        decimal? heightCm,
        int? painScore,
        decimal? bloodGlucoseMgDl,
        Guid? recordedByUserId,
        string? notes,
        Guid? createdBy)
    {
        return new VitalsReading(
            Guid.CreateVersion7(),
            admissionId,
            recordedAt,
            temperatureF,
            pulseRate,
            respiratoryRate,
            bloodPressureSystolic,
            bloodPressureDiastolic,
            spO2Percent,
            weightKg,
            heightCm,
            painScore,
            bloodGlucoseMgDl,
            recordedByUserId,
            notes,
            createdBy);
    }
}
