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

    /// <summary>Resolved server-side from Masters' Diagnosis catalog so the form, print-out and
    /// Patient Details' Medical Information tab can show the name without reading the Masters
    /// endpoints (which require an admin permission a doctor doesn't have). Null if the
    /// diagnosis has since been deleted.</summary>
    public string? DiagnosisName { get; init; }
    public string? IcdCode { get; init; }
}

/// <summary>One investigation line, as submitted on SaveDraft/Complete. The full list is
/// replaced wholesale each time, same convention as diagnoses.</summary>
public record OpdConsultationInvestigationRequest
{
    public string Name { get; init; } = string.Empty;
    public OpdInvestigationDepartment Department { get; init; }
    public OpdInvestigationPriority Priority { get; init; }

    /// <summary>Optional Masters DiagnosticService id — set when the doctor picked the test from
    /// the catalog, so OPD Billing Entry can pre-add it (regression report OPD-01). Null for a
    /// free-text line.</summary>
    public Guid? ServiceId { get; init; }
}

public record OpdConsultationInvestigationResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public OpdInvestigationDepartment Department { get; init; }
    public OpdInvestigationPriority Priority { get; init; }
    public Guid? ServiceId { get; init; }
}

/// <summary>One prescribed medicine, as submitted on SaveDraft/Complete — the full list is
/// replaced wholesale each time, same convention as diagnoses/investigations (regression report
/// OPD-02).</summary>
public record OpdConsultationPrescriptionRequest
{
    public string DrugName { get; init; } = string.Empty;
    public string? Dose { get; init; }
    public string? Route { get; init; }
    public string? Frequency { get; init; }
    public int? DurationDays { get; init; }
    public string? Instructions { get; init; }
}

public record OpdConsultationPrescriptionResponse
{
    public Guid Id { get; init; }
    public string DrugName { get; init; } = string.Empty;
    public string? Dose { get; init; }
    public string? Route { get; init; }
    public string? Frequency { get; init; }
    public int? DurationDays { get; init; }
    public string? Instructions { get; init; }
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
    public IReadOnlyList<OpdConsultationPrescriptionRequest> Prescriptions { get; init; } = [];

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
    public IReadOnlyList<OpdConsultationPrescriptionResponse> Prescriptions { get; init; } = [];

    public string? PlanOfManagement { get; init; }

    public DateOnly? ReviewDate { get; init; }
    public string? FollowUpInstructions { get; init; }

    public string? EmergencyReviewInstructions { get; init; }

    public Guid? ReferralDepartmentId { get; init; }
    public Guid? ReferralConsultantId { get; init; }
    public string? ReferralReason { get; init; }

    /// <summary>When the patient was marked on this consultation (null when not marked) — see
    /// POST /api/v1/opd-consultations/{consultationId}/mark.</summary>
    public DateTime? MarkedAt { get; init; }
    public Guid? MarkedBy { get; init; }
    public string? MarkReason { get; init; }

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

/// <summary>One Diagnosis-catalog entry as the OPD Consultation form's picker shows it — served
/// under the clinical-care permission (regression report OPD-03), since the Masters endpoints
/// require identity-administration, which a doctor doesn't have.</summary>
public record OpdDiagnosisOptionResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? IcdCode { get; init; }
}

/// <summary>Adds a diagnosis to the catalog straight from the consultation when the search finds
/// no match — the catalog "grows as staff use it" (regression report OPD-03).</summary>
public record CreateOpdDiagnosisRequest
{
    public string Name { get; init; } = string.Empty;
    public string? IcdCode { get; init; }
}

/// <summary>One Laboratory/Radiology catalog service as the consultation's investigation picker
/// shows it — no price (not the doctor's concern; billing prices it).</summary>
public record OpdInvestigationServiceOptionResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public OpdInvestigationDepartment Department { get; init; }
}

/// <summary>A catalog-linked investigation the doctor ordered on one visit, as OPD Billing Entry
/// pre-adds it (regression report OPD-01). ConsultantId is the ordering doctor, used as the bill
/// line's referring consultant.</summary>
public record BillableInvestigationResponse
{
    public Guid ConsultationId { get; init; }
    public Guid ConsultantId { get; init; }
    public Guid ServiceId { get; init; }
    public string Name { get; init; } = string.Empty;
    public OpdInvestigationDepartment Department { get; init; }
    public OpdInvestigationPriority Priority { get; init; }
}

/// <summary>
/// One completed consultation that left the doctor's room with medicines prescribed — what
/// the pharmacy needs to fill it: who the patient is, who prescribed, and the drug lines.
/// Read by HMS.Modules.Pharmacy for its prescription queue.
/// </summary>
public record OpdPrescriptionQueueItemResponse
{
    public Guid ConsultationId { get; init; }
    public Guid PatientId { get; init; }
    public string Uhid { get; init; } = string.Empty;
    public string PatientName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string ConsultantName { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;

    /// <summary>When the consultation was completed (the note's last change).</summary>
    public DateTime PrescribedAt { get; init; }
    public IReadOnlyList<OpdConsultationPrescriptionResponse> Prescriptions { get; init; } = [];
}

/// <summary>
/// Filters for the OPD page's Scheduled Visits list — GET /api/v1/opd-consultations/scheduled-visits.
/// A Consultant caller's ConsultantId is forced to their own by the controller.
/// </summary>
public record ScheduledVisitQuery
{
    /// <summary>The first scheduled date to include — the caller's own "today", so the list
    /// starts on the hospital's local calendar day. Defaults to the server's date.</summary>
    public DateOnly? From { get; init; }

    public Guid? DepartmentId { get; init; }
    public Guid? ConsultantId { get; init; }

    /// <summary>Matches patient name, UHID or phone number (contains, case-insensitive).</summary>
    public string? Search { get; init; }
}

/// <summary>
/// One upcoming scheduled visit: the review date a doctor set on an OPD consultation, with the
/// patient and the department/consultant of that consultation — the visit they're due back for.
/// </summary>
public record ScheduledVisitResponse
{
    public Guid ConsultationId { get; init; }
    public Guid PatientId { get; init; }
    public Guid VisitId { get; init; }

    /// <summary>The consultation's review date.</summary>
    public DateOnly ScheduledDate { get; init; }

    public string PatientName { get; init; } = string.Empty;
    public int Age { get; init; }
    public string Gender { get; init; } = string.Empty;
    public string Uhid { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;

    public Guid DepartmentId { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public Guid ConsultantId { get; init; }
    public string ConsultantName { get; init; } = string.Empty;

    /// <summary>The consultation's follow-up instructions, when the doctor wrote any.</summary>
    public string? Notes { get; init; }
}

/// <summary>Marks the consultation's patient — POST /api/v1/opd-consultations/{consultationId}/mark.
/// Posting again while already marked updates the reason.</summary>
public record MarkPatientRequest
{
    /// <summary>Optional: why the patient is being marked.</summary>
    public string? Reason { get; init; }
}

/// <summary>
/// Filters for the OPD page's Marked Patients list — GET /api/v1/opd-consultations/marked-patients.
/// A Consultant caller's ConsultantId is forced to their own by the controller.
/// </summary>
public record MarkedPatientQuery
{
    public Guid? DepartmentId { get; init; }
    public Guid? ConsultantId { get; init; }

    /// <summary>Matches patient name, UHID or phone number (contains, case-insensitive).</summary>
    public string? Search { get; init; }
}

/// <summary>
/// One marked patient: a consultation whose patient the doctor marked, with the patient, the
/// department/consultant of that consultation and the reason given.
/// </summary>
public record MarkedPatientResponse
{
    public Guid ConsultationId { get; init; }
    public Guid PatientId { get; init; }
    public Guid VisitId { get; init; }

    public DateTime MarkedAt { get; init; }
    public string? Reason { get; init; }

    /// <summary>The consultation's own date and time.</summary>
    public DateTime AppointmentTime { get; init; }

    public string PatientName { get; init; } = string.Empty;
    public int Age { get; init; }
    public string Gender { get; init; } = string.Empty;
    public string Uhid { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;

    public Guid DepartmentId { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public Guid ConsultantId { get; init; }
    public string ConsultantName { get; init; } = string.Empty;
}
