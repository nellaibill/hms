namespace HMS.Modules.DischargeSummary.Application;

/// <summary>
/// Stable, machine-readable error codes for expected DischargeSummary-module failures, per
/// docs/ApiStandards.md §5 — the UI branches on these, not on the message text.
/// </summary>
internal static class DischargeSummaryErrorCodes
{
    public const string NotFound = "DISCHARGE_SUMMARY.NOT_FOUND";

    /// <summary>The referenced Admission does not exist (IAdmissionService.GetByIdAsync
    /// returned a failure).</summary>
    public const string InvalidAdmission = "DISCHARGE_SUMMARY.INVALID_ADMISSION";

    /// <summary>Create was attempted for an Admission whose Status isn't Discharged yet —
    /// see docs/DecisionLog.md's creation-gate decision.</summary>
    public const string AdmissionNotDischarged = "DISCHARGE_SUMMARY.ADMISSION_NOT_DISCHARGED";

    /// <summary>A DischargeSummary already exists for this AdmissionId — one per admission,
    /// enforced here (check-then-create) with a unique index as a backstop.</summary>
    public const string AlreadyExists = "DISCHARGE_SUMMARY.ALREADY_EXISTS";

    /// <summary>The Admission's own PatientId does not resolve via IPatientService —
    /// defense-in-depth (the patient could theoretically be soft-deleted after admission).</summary>
    public const string InvalidPatient = "DISCHARGE_SUMMARY.INVALID_PATIENT";

    /// <summary>Update was attempted while Status is already Finalized.</summary>
    public const string NotDraft = "DISCHARGE_SUMMARY.NOT_DRAFT";

    /// <summary>Finalize was attempted while Status is already Finalized.</summary>
    public const string AlreadyFinalized = "DISCHARGE_SUMMARY.ALREADY_FINALIZED";
}
