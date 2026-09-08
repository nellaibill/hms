using HMS.Modules.DischargeSummary.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.DischargeSummary.Domain;

/// <summary>
/// The discharge summary aggregate root for one IPD admission. AdmissionId/PatientId are
/// bare Guids validated at the Application layer through IPD's/Patients' public service
/// interfaces — never a real cross-schema DB foreign key (docs/DatabaseArchitecture.md §7,
/// mirrors HMS.Modules.Pharmacy.Domain.PharmacyStockTransaction's identical treatment of
/// PatientId/AdmissionId). Draft → Finalized is the only state transition; once Finalized,
/// the caller (DischargeSummaryService) is responsible for rejecting further updates — this
/// entity itself does not re-check its own Status before applying a mutation, same
/// convention as HMS.Modules.IPD.Domain.Admission.TransferBed/Discharge.
/// </summary>
internal class DischargeSummary : Entity
{
    public Guid AdmissionId { get; private set; }
    public Guid PatientId { get; private set; }
    public DischargeSummaryStatus Status { get; private set; }

    /// <summary>Pre-filled from Admission.FinalDiagnosis at <see cref="Create"/> time by the
    /// caller (DischargeSummaryService) — independently editable from that point via
    /// UpdateClinicalDetails, never re-read from Admission after creation.</summary>
    public string? FinalDiagnosis { get; private set; }

    // Clinical.
    public string? ChiefComplaints { get; private set; }
    public string? HistoryOfPresentingIllness { get; private set; }
    public string? PastMedicalHistory { get; private set; }
    public string? PastSurgicalHistory { get; private set; }
    public string? FamilyHistory { get; private set; }
    public string? PersonalHistory { get; private set; }

    // Examination — mirrors the source PDF's actual section headings rather than one blob.
    public string? GeneralExamination { get; private set; }
    public string? CvsFindings { get; private set; }
    public string? RsFindings { get; private set; }
    public string? PaFindings { get; private set; }
    public string? CnsFindings { get; private set; }
    public string? LocalExamination { get; private set; }
    public string? Gait { get; private set; }

    // Vitals — a single snapshot, not a repeating observations table (out of scope per the
    // approved plan).
    public decimal? HeightCm { get; private set; }
    public decimal? WeightKg { get; private set; }
    public int? PulseRate { get; private set; }
    public int? RespiratoryRate { get; private set; }
    public decimal? TemperatureF { get; private set; }
    public int? SpO2Percent { get; private set; }
    public string? BloodPressure { get; private set; }

    /// <summary>Single narrative field — the structured chronological-event alternative
    /// needs richer IPD daily-event data that doesn't exist yet (deferred phase per the
    /// approved plan).</summary>
    public string? CourseInHospital { get; private set; }

    // Surgical Details — plain manual fields; there is no OT module yet to integrate with
    // (Modules/OT has zero files, confirmed at plan time).
    public string? ProcedureName { get; private set; }
    public DateTime? ProcedureDateTime { get; private set; }
    public string? PrimarySurgeon { get; private set; }
    public string? AssistantSurgeons { get; private set; }
    public string? Anaesthetist { get; private set; }
    public string? Anaesthesia { get; private set; }
    public string? SurgicalPosition { get; private set; }
    public string? IntraOperativeFindings { get; private set; }
    public string? OperativeNotes { get; private set; }

    // Discharge Advice.
    public string? Diet { get; private set; }
    public string? WoundCare { get; private set; }
    public string? Activity { get; private set; }
    public string? Physiotherapy { get; private set; }
    public string? ReviewInstructions { get; private set; }
    public string? EmergencyInstructions { get; private set; }
    public string? ConditionAtDischarge { get; private set; }

    public Guid? PreparedByUserId { get; private set; }
    public Guid? CheckedByUserId { get; private set; }
    public Guid? ConsultantApprovedByUserId { get; private set; }
    public DateTime? FinalizedAt { get; private set; }
    public Guid? FinalizedByUserId { get; private set; }

    // Required by EF Core materialization.
    private DischargeSummary()
    {
    }

    private DischargeSummary(Guid id, Guid admissionId, Guid patientId, string? finalDiagnosis, Guid? createdBy)
        : base(id, createdBy)
    {
        AdmissionId = admissionId;
        PatientId = patientId;
        FinalDiagnosis = Normalize(finalDiagnosis);
        Status = DischargeSummaryStatus.Draft;
    }

    /// <summary>Caller (DischargeSummaryService) is responsible for confirming the admission
    /// is Discharged and that no other DischargeSummary already exists for it before calling
    /// this — see docs/DecisionLog.md.</summary>
    public static DischargeSummary Create(Guid admissionId, Guid patientId, string? finalDiagnosisPrefill, Guid? createdBy)
        // Time-ordered UUID per docs/DatabaseArchitecture.md §4.
        => new(Guid.CreateVersion7(), admissionId, patientId, finalDiagnosisPrefill, createdBy);

    /// <summary>Replaces every clinical/examination/vitals/course/surgical/advice field in
    /// one call — the PUT endpoint is a full-record update, not per-field PATCH (matches the
    /// approved plan's MVP scope). Caller (DischargeSummaryService) is responsible for
    /// rejecting this call while Status isn't Draft — this entity does not re-check its own
    /// Status, same convention as <see cref="Finalize"/> and
    /// HMS.Modules.IPD.Domain.Admission.TransferBed/Discharge. Free-text fields are trimmed
    /// and normalized to null when blank, so "cleared" and "never entered" read identically.
    /// </summary>
    public void UpdateClinicalDetails(
        string? finalDiagnosis,
        string? chiefComplaints,
        string? historyOfPresentingIllness,
        string? pastMedicalHistory,
        string? pastSurgicalHistory,
        string? familyHistory,
        string? personalHistory,
        string? generalExamination,
        string? cvsFindings,
        string? rsFindings,
        string? paFindings,
        string? cnsFindings,
        string? localExamination,
        string? gait,
        decimal? heightCm,
        decimal? weightKg,
        int? pulseRate,
        int? respiratoryRate,
        decimal? temperatureF,
        int? spO2Percent,
        string? bloodPressure,
        string? courseInHospital,
        string? procedureName,
        DateTime? procedureDateTime,
        string? primarySurgeon,
        string? assistantSurgeons,
        string? anaesthetist,
        string? anaesthesia,
        string? surgicalPosition,
        string? intraOperativeFindings,
        string? operativeNotes,
        string? diet,
        string? woundCare,
        string? activity,
        string? physiotherapy,
        string? reviewInstructions,
        string? emergencyInstructions,
        string? conditionAtDischarge,
        Guid? updatedBy)
    {
        FinalDiagnosis = Normalize(finalDiagnosis);
        ChiefComplaints = Normalize(chiefComplaints);
        HistoryOfPresentingIllness = Normalize(historyOfPresentingIllness);
        PastMedicalHistory = Normalize(pastMedicalHistory);
        PastSurgicalHistory = Normalize(pastSurgicalHistory);
        FamilyHistory = Normalize(familyHistory);
        PersonalHistory = Normalize(personalHistory);

        GeneralExamination = Normalize(generalExamination);
        CvsFindings = Normalize(cvsFindings);
        RsFindings = Normalize(rsFindings);
        PaFindings = Normalize(paFindings);
        CnsFindings = Normalize(cnsFindings);
        LocalExamination = Normalize(localExamination);
        Gait = Normalize(gait);

        HeightCm = heightCm;
        WeightKg = weightKg;
        PulseRate = pulseRate;
        RespiratoryRate = respiratoryRate;
        TemperatureF = temperatureF;
        SpO2Percent = spO2Percent;
        BloodPressure = Normalize(bloodPressure);

        CourseInHospital = Normalize(courseInHospital);

        ProcedureName = Normalize(procedureName);
        ProcedureDateTime = procedureDateTime;
        PrimarySurgeon = Normalize(primarySurgeon);
        AssistantSurgeons = Normalize(assistantSurgeons);
        Anaesthetist = Normalize(anaesthetist);
        Anaesthesia = Normalize(anaesthesia);
        SurgicalPosition = Normalize(surgicalPosition);
        IntraOperativeFindings = Normalize(intraOperativeFindings);
        OperativeNotes = Normalize(operativeNotes);

        Diet = Normalize(diet);
        WoundCare = Normalize(woundCare);
        Activity = Normalize(activity);
        Physiotherapy = Normalize(physiotherapy);
        ReviewInstructions = Normalize(reviewInstructions);
        EmergencyInstructions = Normalize(emergencyInstructions);
        ConditionAtDischarge = Normalize(conditionAtDischarge);

        MarkUpdated(updatedBy);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Caller (DischargeSummaryService) is responsible for rejecting this call while
    /// already Finalized.</summary>
    public void Finalize(
        Guid? preparedByUserId,
        Guid? checkedByUserId,
        Guid? consultantApprovedByUserId,
        DateTime finalizedAt,
        Guid? finalizedByUserId)
    {
        Status = DischargeSummaryStatus.Finalized;
        PreparedByUserId = preparedByUserId;
        CheckedByUserId = checkedByUserId;
        ConsultantApprovedByUserId = consultantApprovedByUserId;
        FinalizedAt = finalizedAt;
        FinalizedByUserId = finalizedByUserId;
        MarkUpdated(finalizedByUserId);
    }
}
