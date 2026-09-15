using HMS.Modules.OpdConsultation.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.OpdConsultation.Domain;

/// <summary>
/// The OPD Consultation clinical note aggregate root — one per PatientVisitConsultation (1:1,
/// enforced by a unique index on ConsultationId, not a DB FK: ConsultationId/PatientId/VisitId
/// are app-level references into Patients' schema, validated through that module's public
/// IOpdQueryService, never a real cross-schema foreign key, same convention
/// HMS.Modules.DischargeSummary.Domain.DischargeSummary's AdmissionId/PatientId already use).
/// Draft -> Completed is the only state transition; once Completed, the caller
/// (OpdConsultationService) is responsible for rejecting further SaveDraft calls — this entity
/// itself does not re-check its own Status before applying a mutation, same convention as
/// DischargeSummary.UpdateClinicalDetails/HMS.Modules.IPD.Domain.Admission.TransferBed.
/// </summary>
internal class OpdConsultationNote : Entity
{
    public Guid ConsultationId { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid VisitId { get; private set; }
    public OpdConsultationNoteStatus Status { get; private set; }

    // Vitals — a single snapshot per visit, not a repeating observations table (unlike IPD's
    // VitalsReading). BMI is deliberately never stored here — computed client-side from
    // Height/Weight, matching this codebase's existing convention (no entity anywhere stores a
    // BMI field).
    public decimal? HeightCm { get; private set; }
    public decimal? WeightKg { get; private set; }
    public int? PulseRate { get; private set; }
    public string? BloodPressure { get; private set; }
    public decimal? TemperatureF { get; private set; }
    public int? SpO2Percent { get; private set; }

    // Clinical assessment.
    public string? PresentingComplaints { get; private set; }
    public string? ClinicalHistory { get; private set; }
    public string? ExaminationFindings { get; private set; }

    public string? PlanOfManagement { get; private set; }

    // Follow-up / review.
    public DateOnly? ReviewDate { get; private set; }
    public string? FollowUpInstructions { get; private set; }

    public string? EmergencyReviewInstructions { get; private set; }

    // Referral.
    public Guid? ReferralDepartmentId { get; private set; }
    public Guid? ReferralConsultantId { get; private set; }
    public string? ReferralReason { get; private set; }

    /// <summary>1:many child, real DB FK — see OpdConsultationDiagnosis's own doc comment.
    /// Only ever loaded/replaced through this aggregate.</summary>
    private readonly List<OpdConsultationDiagnosis> _diagnoses = [];
    public IReadOnlyCollection<OpdConsultationDiagnosis> Diagnoses => _diagnoses.AsReadOnly();

    private readonly List<OpdConsultationInvestigation> _investigations = [];
    public IReadOnlyCollection<OpdConsultationInvestigation> Investigations => _investigations.AsReadOnly();

    // Required by EF Core materialization.
    private OpdConsultationNote()
    {
    }

    private OpdConsultationNote(Guid id, Guid consultationId, Guid patientId, Guid visitId, Guid? createdBy)
        : base(id, createdBy)
    {
        ConsultationId = consultationId;
        PatientId = patientId;
        VisitId = visitId;
        Status = OpdConsultationNoteStatus.Draft;
    }

    /// <summary>Caller (OpdConsultationService) is responsible for confirming the consultation
    /// exists and that no note already exists for it before calling this.</summary>
    public static OpdConsultationNote Create(Guid consultationId, Guid patientId, Guid visitId, Guid? createdBy)
        // Time-ordered UUID per docs/DatabaseArchitecture.md §4.
        => new(Guid.CreateVersion7(), consultationId, patientId, visitId, createdBy);

    /// <summary>Replaces every vitals/assessment/plan/follow-up/referral field in one call — a
    /// full-record update, not per-field PATCH (matches DischargeSummary's identical MVP
    /// scope). Used by both SaveDraft and Complete; the caller decides which fields are
    /// required before calling (SaveDraft: none: Complete: PresentingComplaints/HeightCm/
    /// WeightKg) — this entity does not re-validate. Caller is responsible for rejecting this
    /// call while Status isn't Draft.</summary>
    public void SaveDetails(
        decimal? heightCm,
        decimal? weightKg,
        int? pulseRate,
        string? bloodPressure,
        decimal? temperatureF,
        int? spO2Percent,
        string? presentingComplaints,
        string? clinicalHistory,
        string? examinationFindings,
        string? planOfManagement,
        DateOnly? reviewDate,
        string? followUpInstructions,
        string? emergencyReviewInstructions,
        Guid? referralDepartmentId,
        Guid? referralConsultantId,
        string? referralReason,
        Guid? updatedBy)
    {
        HeightCm = heightCm;
        WeightKg = weightKg;
        PulseRate = pulseRate;
        BloodPressure = Normalize(bloodPressure);
        TemperatureF = temperatureF;
        SpO2Percent = spO2Percent;

        PresentingComplaints = Normalize(presentingComplaints);
        ClinicalHistory = Normalize(clinicalHistory);
        ExaminationFindings = Normalize(examinationFindings);

        PlanOfManagement = Normalize(planOfManagement);

        ReviewDate = reviewDate;
        FollowUpInstructions = Normalize(followUpInstructions);

        EmergencyReviewInstructions = Normalize(emergencyReviewInstructions);

        ReferralDepartmentId = referralDepartmentId;
        ReferralConsultantId = referralConsultantId;
        ReferralReason = Normalize(referralReason);

        MarkUpdated(updatedBy);
    }

    /// <summary>Fully replaces the diagnosis list — delete-then-reinsert list-sync, no separate
    /// per-line CRUD. Caller (OpdConsultationService) constructs each new
    /// OpdConsultationDiagnosis beforehand, already carrying this aggregate's Id.</summary>
    public void ReplaceDiagnoses(IEnumerable<OpdConsultationDiagnosis> diagnoses, Guid? updatedBy)
    {
        _diagnoses.Clear();
        _diagnoses.AddRange(diagnoses);
        MarkUpdated(updatedBy);
    }

    public void ReplaceInvestigations(IEnumerable<OpdConsultationInvestigation> investigations, Guid? updatedBy)
    {
        _investigations.Clear();
        _investigations.AddRange(investigations);
        MarkUpdated(updatedBy);
    }

    /// <summary>Caller (OpdConsultationService) is responsible for rejecting this call while
    /// already Completed, and for calling Patients' PatientVisitConsultation.Complete()
    /// transition alongside this — the two are advanced together, not by this entity.</summary>
    public void Complete(Guid? updatedBy)
    {
        Status = OpdConsultationNoteStatus.Completed;
        MarkUpdated(updatedBy);
    }

    /// <summary>The inverse of Complete — moves a Completed note back to Draft so it can be
    /// edited again. Caller (OpdConsultationService) is responsible for confirming Status is
    /// currently Completed before calling this, and for calling Patients'
    /// PatientVisitConsultation.Reopen() transition alongside it, same as Complete does going
    /// the other direction.</summary>
    public void Reopen(Guid? updatedBy)
    {
        Status = OpdConsultationNoteStatus.Draft;
        MarkUpdated(updatedBy);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
