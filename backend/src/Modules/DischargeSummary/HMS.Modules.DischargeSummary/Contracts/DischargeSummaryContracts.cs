namespace HMS.Modules.DischargeSummary.Contracts;

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

    public Guid? PreparedByUserId { get; init; }
    public Guid? CheckedByUserId { get; init; }
    public Guid? ConsultantApprovedByUserId { get; init; }
    public DateTime? FinalizedAt { get; init; }
    public Guid? FinalizedByUserId { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
