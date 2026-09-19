namespace HMS.Modules.DischargeSummary.Contracts;

/// <summary>
/// One discharge medication line, as submitted on Update. The full list is replaced
/// wholesale each time (list-sync, no separate per-line CRUD) — see
/// DischargeSummary.ReplaceMedications.
/// </summary>
public record DischargeMedicationRequest
{
    public int SortOrder { get; init; }
    public string DrugName { get; init; } = string.Empty;
    public string Dose { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public decimal MorningQty { get; init; }
    public decimal NoonQty { get; init; }
    public decimal EveningQty { get; init; }
    public decimal NightQty { get; init; }
    public int DurationDays { get; init; }
    public FoodInstruction FoodInstruction { get; init; }
}

/// <summary>One discharge medication line, as returned on Get/Update/Finalize.</summary>
public record DischargeMedicationResponse
{
    public Guid Id { get; init; }
    public int SortOrder { get; init; }
    public string DrugName { get; init; } = string.Empty;
    public string Dose { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public decimal MorningQty { get; init; }
    public decimal NoonQty { get; init; }
    public decimal EveningQty { get; init; }
    public decimal NightQty { get; init; }
    public int DurationDays { get; init; }
    public FoodInstruction FoodInstruction { get; init; }
}

/// <summary>
/// Full-record update — every clinical/examination/vitals/course/surgical/advice field,
/// submitted together on every PUT (matches the approved plan's MVP scope: no per-field
/// PATCH). Only allowed while Status is Draft; rejected once Finalized
/// (DischargeSummaryErrorCodes.NotDraft).
/// </summary>
public record UpdateDischargeSummaryRequest
{
    /// <summary>Independently editable from its Create-time prefill (Admission.
    /// FinalDiagnosis) — see docs/DecisionLog.md.</summary>
    public string? FinalDiagnosis { get; init; }

    // Clinical.
    public string? ChiefComplaints { get; init; }
    public string? HistoryOfPresentingIllness { get; init; }
    public string? PastMedicalHistory { get; init; }
    public string? PastSurgicalHistory { get; init; }
    public string? FamilyHistory { get; init; }
    public string? PersonalHistory { get; init; }

    // Examination.
    public string? GeneralExamination { get; init; }
    public string? CvsFindings { get; init; }
    public string? RsFindings { get; init; }
    public string? PaFindings { get; init; }
    public string? CnsFindings { get; init; }
    public string? LocalExamination { get; init; }
    public string? Gait { get; init; }

    // Vitals — a single snapshot, not a repeating observations table.
    public decimal? HeightCm { get; init; }
    public decimal? WeightKg { get; init; }
    public int? PulseRate { get; init; }
    public int? RespiratoryRate { get; init; }
    public decimal? TemperatureF { get; init; }
    public int? SpO2Percent { get; init; }

    /// <summary>Free-text, e.g. "130/80".</summary>
    public string? BloodPressure { get; init; }

    public string? CourseInHospital { get; init; }

    // Surgical Details — plain manual fields (no OT module yet).
    public string? ProcedureName { get; init; }
    public DateTime? ProcedureDateTime { get; init; }
    public string? PrimarySurgeon { get; init; }
    public string? AssistantSurgeons { get; init; }
    public string? Anaesthetist { get; init; }
    public string? Anaesthesia { get; init; }
    public string? SurgicalPosition { get; init; }
    public string? IntraOperativeFindings { get; init; }
    public string? OperativeNotes { get; init; }

    // Discharge Advice.
    public string? Diet { get; init; }
    public string? WoundCare { get; init; }
    public string? Activity { get; init; }
    public string? Physiotherapy { get; init; }
    public string? ReviewInstructions { get; init; }
    public string? EmergencyInstructions { get; init; }
    public string? ConditionAtDischarge { get; init; }

    /// <summary>The full medication list — replaces whatever was there before in one call
    /// (list-sync, no separate per-line CRUD). An empty list clears every medication line.
    /// </summary>
    public IReadOnlyList<DischargeMedicationRequest> Medications { get; init; } = [];
}

/// <summary>
/// Sign-off captured once, at Finalize — not a multi-step approval workflow (see
/// docs/DecisionLog.md). All three fields are optional: a hospital that only ever fills in
/// "Prepared By" still gets a valid, finalized summary.
/// </summary>
public record FinalizeDischargeSummaryRequest
{
    public Guid? PreparedByUserId { get; init; }
    public Guid? CheckedByUserId { get; init; }
    public Guid? ConsultantApprovedByUserId { get; init; }
}

/// <summary>
/// Deliberately carries only this aggregate's own authored content — no live-joined
/// Patient/Admission display fields (name, UHID, ward/bed, consultant, etc.). Per the
/// approved plan, those are fetched separately by the frontend from
/// GET /api/v1/patients/{id} and GET /api/v1/ipd/admissions/{id} at render time, never
/// duplicated here.
/// </summary>
public record DischargeSummaryResponse
{
    public Guid Id { get; init; }
    public Guid AdmissionId { get; init; }
    public Guid PatientId { get; init; }
    public DischargeSummaryStatus Status { get; init; }

    /// <summary>Pre-filled from Admission.FinalDiagnosis at Create time, independently
    /// editable from that point — see docs/DecisionLog.md.</summary>
    public string? FinalDiagnosis { get; init; }

    // Clinical.
    public string? ChiefComplaints { get; init; }
    public string? HistoryOfPresentingIllness { get; init; }
    public string? PastMedicalHistory { get; init; }
    public string? PastSurgicalHistory { get; init; }
    public string? FamilyHistory { get; init; }
    public string? PersonalHistory { get; init; }

    // Examination.
    public string? GeneralExamination { get; init; }
    public string? CvsFindings { get; init; }
    public string? RsFindings { get; init; }
    public string? PaFindings { get; init; }
    public string? CnsFindings { get; init; }
    public string? LocalExamination { get; init; }
    public string? Gait { get; init; }

    // Vitals.
    public decimal? HeightCm { get; init; }
    public decimal? WeightKg { get; init; }
    public int? PulseRate { get; init; }
    public int? RespiratoryRate { get; init; }
    public decimal? TemperatureF { get; init; }
    public int? SpO2Percent { get; init; }
    public string? BloodPressure { get; init; }

    public string? CourseInHospital { get; init; }

    // Surgical Details.
    public string? ProcedureName { get; init; }
    public DateTime? ProcedureDateTime { get; init; }
    public string? PrimarySurgeon { get; init; }
    public string? AssistantSurgeons { get; init; }
    public string? Anaesthetist { get; init; }
    public string? Anaesthesia { get; init; }
    public string? SurgicalPosition { get; init; }
    public string? IntraOperativeFindings { get; init; }
    public string? OperativeNotes { get; init; }

    // Discharge Advice.
    public string? Diet { get; init; }
    public string? WoundCare { get; init; }
    public string? Activity { get; init; }
    public string? Physiotherapy { get; init; }
    public string? ReviewInstructions { get; init; }
    public string? EmergencyInstructions { get; init; }
    public string? ConditionAtDischarge { get; init; }

    public IReadOnlyList<DischargeMedicationResponse> Medications { get; init; } = [];

    public Guid? PreparedByUserId { get; init; }
    public Guid? CheckedByUserId { get; init; }
    public Guid? ConsultantApprovedByUserId { get; init; }
    public DateTime? FinalizedAt { get; init; }
    public Guid? FinalizedByUserId { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// AI-suggested values for a Draft discharge summary — never persisted by the call that produces
/// it; the form merges these into its own state and still saves through Update. Narrative fields
/// come from the AI provider; the vitals fields are copied verbatim from the admission's most
/// recent IPD vitals reading (no AI involved). Discharge medications are deliberately absent:
/// inpatient orders are not discharge prescriptions.
/// </summary>
public record DischargeSummaryDraftSuggestion
{
    public string? ChiefComplaints { get; init; }
    public string? HistoryOfPresentingIllness { get; init; }
    public string? CourseInHospital { get; init; }
    public string? ConditionAtDischarge { get; init; }
    public string? Diet { get; init; }
    public string? WoundCare { get; init; }
    public string? Activity { get; init; }
    public string? Physiotherapy { get; init; }
    public string? ReviewInstructions { get; init; }
    public string? EmergencyInstructions { get; init; }

    public decimal? HeightCm { get; init; }
    public decimal? WeightKg { get; init; }
    public int? PulseRate { get; init; }
    public int? RespiratoryRate { get; init; }
    public decimal? TemperatureF { get; init; }
    public int? SpO2Percent { get; init; }
    public string? BloodPressure { get; init; }
}
