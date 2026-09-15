namespace HMS.Modules.OpdConsultation.Contracts;

/// <summary>Draft while the doctor is still documenting; Completed once "Complete Consultation"
/// is clicked — at which point CompleteAsync also advances the owning
/// PatientVisitConsultation's own queue status via Patients' public IOpdQueryService, so the
/// two stay in lockstep. No richer workflow than this two-state split, matching
/// DischargeSummaryStatus's identical Draft/Finalized precedent.</summary>
public enum OpdConsultationNoteStatus
{
    Draft,
    Completed,
}

/// <summary>Whether a recorded diagnosis is the primary reason for this consultation or a
/// secondary/incidental finding.</summary>
public enum OpdDiagnosisType
{
    Primary,
    Secondary,
}

/// <summary>Which workflow an investigation belongs to — the only two catalogs an OPD doctor
/// can request into (matches ConsultationBillingCard's own Laboratory/Radiology split).</summary>
public enum OpdInvestigationDepartment
{
    Laboratory,
    Radiology,
}

public enum OpdInvestigationPriority
{
    Routine,
    Urgent,
    Stat,
}
