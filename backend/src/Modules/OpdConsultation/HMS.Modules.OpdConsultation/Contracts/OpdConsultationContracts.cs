namespace HMS.Modules.OpdConsultation.Contracts;

/// <summary>One diagnosis line, as submitted on SaveDraft/Complete. The full list is replaced
/// wholesale each time (list-sync, no separate per-line CRUD) — see
/// OpdConsultationNote.ReplaceDiagnoses.</summary>
public record OpdConsultationDiagnosisRequest
{
    public Guid DiagnosisId { get; init; }
    public OpdDiagnosisType Type { get; init; }
}

public record OpdConsultationDiagnosisResponse
{
    public Guid Id { get; init; }
    public Guid DiagnosisId { get; init; }
    public OpdDiagnosisType Type { get; init; }
}

/// <summary>One investigation line, as submitted on SaveDraft/Complete. The full list is
/// replaced wholesale each time, same convention as diagnoses.</summary>
public record OpdConsultationInvestigationRequest
{
    public string Name { get; init; } = string.Empty;
    public OpdInvestigationDepartment Department { get; init; }
    public OpdInvestigationPriority Priority { get; init; }
}

public record OpdConsultationInvestigationResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public OpdInvestigationDepartment Department { get; init; }
    public OpdInvestigationPriority Priority { get; init; }
}

/// <summary>
/// Full-record save — every vitals/assessment/plan/follow-up/referral field, submitted together
/// on both SaveDraft and Complete (matches DischargeSummary's identical "PUT is a full-record
/// update" MVP scope: no per-field PATCH). SaveDraft applies no required-field validation;
/// Complete additionally requires PresentingComplaints/HeightCm/WeightKg (enforced by
/// CompleteOpdConsultationRequestValidator, not this shared shape).
/// </summary>
public record SaveOpdConsultationRequest
{
    // Vitals.
    public decimal? HeightCm { get; init; }
    public decimal? WeightKg { get; init; }
    public int? PulseRate { get; init; }

    /// <summary>Free-text, e.g. "120/80" — a single snapshot per visit, not IPD's repeating,
    /// separately-systolic/diastolic VitalsReading log.</summary>
    public string? BloodPressure { get; init; }
    public decimal? TemperatureF { get; init; }
    public int? SpO2Percent { get; init; }

    // Clinical assessment.
    public string? PresentingComplaints { get; init; }
    public string? ClinicalHistory { get; init; }
    public string? ExaminationFindings { get; init; }

    public IReadOnlyList<OpdConsultationDiagnosisRequest> Diagnoses { get; init; } = [];
    public IReadOnlyList<OpdConsultationInvestigationRequest> Investigations { get; init; } = [];

    public string? PlanOfManagement { get; init; }

    // Follow-up / review.
    public DateOnly? ReviewDate { get; init; }
    public string? FollowUpInstructions { get; init; }

    public string? EmergencyReviewInstructions { get; init; }

    // Referral — structured (real Department/Consultant references), unlike IPD DoctorOrder's
    // free-text-only Referral type.
    public Guid? ReferralDepartmentId { get; init; }
    public Guid? ReferralConsultantId { get; init; }
    public string? ReferralReason { get; init; }
}

/// <summary>
/// Deliberately carries only this note's own authored content — no live-joined Patient/
/// Consultant/Department display fields. Those come from OpdConsultationHeader below, fetched
/// via Patients' public IOpdQueryService, never duplicated here — same separation
/// DischargeSummaryResponse's own doc comment describes for Patient/Admission fields.
/// </summary>
public record OpdConsultationNoteResponse
{
    public Guid? Id { get; init; }
    public Guid ConsultationId { get; init; }
    public OpdConsultationNoteStatus Status { get; init; }

    public decimal? HeightCm { get; init; }
    public decimal? WeightKg { get; init; }
    public int? PulseRate { get; init; }
    public string? BloodPressure { get; init; }
    public decimal? TemperatureF { get; init; }
    public int? SpO2Percent { get; init; }

    public string? PresentingComplaints { get; init; }
    public string? ClinicalHistory { get; init; }
    public string? ExaminationFindings { get; init; }

    public IReadOnlyList<OpdConsultationDiagnosisResponse> Diagnoses { get; init; } = [];
    public IReadOnlyList<OpdConsultationInvestigationResponse> Investigations { get; init; } = [];

    public string? PlanOfManagement { get; init; }

    public DateOnly? ReviewDate { get; init; }
    public string? FollowUpInstructions { get; init; }

    public string? EmergencyReviewInstructions { get; init; }

    public Guid? ReferralDepartmentId { get; init; }
    public Guid? ReferralConsultantId { get; init; }
    public string? ReferralReason { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>The read-only header the form's top strip renders — patient/appointment/
/// consultant/department, resolved from Patients' OpdPatientListItem (see
/// IOpdConsultationService.GetOrCreateByConsultationIdAsync).</summary>
public record OpdConsultationHeader
{
    public Guid ConsultationId { get; init; }
    public Guid VisitId { get; init; }
    public Guid PatientId { get; init; }
    public string Uhid { get; init; } = string.Empty;
    public string PatientName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public int Age { get; init; }
    public string Gender { get; init; } = string.Empty;
    public DateTime AppointmentTime { get; init; }
    public Guid DepartmentId { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public Guid ConsultantId { get; init; }
    public string ConsultantName { get; init; } = string.Empty;
    /// <summary>The owning PatientVisitConsultation's own queue status (Waiting/CheckedIn/
    /// InConsultation/Completed/Cancelled/NoShow) — distinct from OpdConsultationNoteResponse.
    /// Status above, which only ever tracks this note's own Draft/Completed state.</summary>
    public string ConsultationStatus { get; init; } = string.Empty;
}

/// <summary>The form's one "give me everything I need to render" response — the note (or an
/// empty Draft shape, auto-created on first fetch) plus the read-only header.</summary>
public record OpdConsultationDetailResponse
{
    public OpdConsultationHeader Header { get; init; } = new();
    public OpdConsultationNoteResponse Note { get; init; } = new();
}

/// <summary>A raw dictation/typed transcript to structure into note fields — never persisted by
/// itself, only ever the input to IClinicalNoteAiClient.StructureAsync.</summary>
public record StructureConsultationNoteRequest
{
    public string Transcript { get; init; } = string.Empty;
}

/// <summary>The narrative fields IClinicalNoteAiClient extracted from a transcript — the caller
/// (the OPD Consultation form) merges these into its own draft state and still submits them
/// through the normal SaveDraft/Complete calls; nothing here is saved directly.</summary>
public record StructuredConsultationNoteResponse
{
    public string? PresentingComplaints { get; init; }
    public string? ClinicalHistory { get; init; }
    public string? ExaminationFindings { get; init; }
    public string? PlanOfManagement { get; init; }
    public string? FollowUpInstructions { get; init; }
    public string? EmergencyReviewInstructions { get; init; }
}
