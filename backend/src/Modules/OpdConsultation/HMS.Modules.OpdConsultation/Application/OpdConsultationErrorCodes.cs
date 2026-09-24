namespace HMS.Modules.OpdConsultation.Application;

/// <summary>
/// Stable, machine-readable error codes for expected OpdConsultation-module failures, per
/// docs/ApiStandards.md §5 — the UI branches on these, not on the message text.
/// </summary>
internal static class OpdConsultationErrorCodes
{
    public const string NotFound = "OPD_CONSULTATION.NOT_FOUND";

    /// <summary>The referenced PatientVisitConsultation does not exist (Patients'
    /// IOpdQueryService.GetConsultationDetailAsync returned a failure).</summary>
    public const string InvalidConsultation = "OPD_CONSULTATION.INVALID_CONSULTATION";

    /// <summary>One of the submitted DiagnosisIds does not exist in Masters' Diagnosis
    /// catalog.</summary>
    public const string InvalidDiagnosis = "OPD_CONSULTATION.INVALID_DIAGNOSIS";

    /// <summary>ReferralDepartmentId/ReferralConsultantId does not resolve via Masters'
    /// public services.</summary>
    public const string InvalidReferral = "OPD_CONSULTATION.INVALID_REFERRAL";

    /// <summary>An investigation's ServiceId doesn't resolve to an active Masters
    /// DiagnosticService of the line's own department (Laboratory/Radiology).</summary>
    public const string InvalidInvestigation = "OPD_CONSULTATION.INVALID_INVESTIGATION";

    /// <summary>SaveDraft was attempted while Status is already Completed.</summary>
    public const string NotDraft = "OPD_CONSULTATION.NOT_DRAFT";

    /// <summary>Complete was attempted while Status is already Completed.</summary>
    public const string AlreadyCompleted = "OPD_CONSULTATION.ALREADY_COMPLETED";

    /// <summary>Complete was attempted without PresentingComplaints/HeightCm/WeightKg set —
    /// SaveDraft has no such requirement, only Complete does (matches the form's own `*`
    /// markers), so this is checked in OpdConsultationService.CompleteAsync directly rather
    /// than via a second FluentValidation validator for the same request shape (which would
    /// collide with SaveDraft's lenient one in DI, both being IValidator&lt;
    /// SaveOpdConsultationRequest&gt;).</summary>
    public const string MissingRequiredFieldsForCompletion = "OPD_CONSULTATION.MISSING_REQUIRED_FIELDS";

    /// <summary>Reopen was attempted on a note that isn't currently Completed.</summary>
    public const string NotCompleted = "OPD_CONSULTATION.NOT_COMPLETED";

    /// <summary>StructureNoteFromTranscriptAsync was attempted but Ai:Anthropic:ApiKey isn't
    /// configured — unlike Notifications' Sms/Email channels, this isn't best-effort: the
    /// caller is actively waiting on a result, so this must surface as a real error rather than
    /// a silent no-op.</summary>
    public const string AiNotConfigured = "OPD_CONSULTATION.AI_NOT_CONFIGURED";

    /// <summary>The call to the AI provider failed (network error, non-success response, or an
    /// unparseable/incomplete tool-use result).</summary>
    public const string AiRequestFailed = "OPD_CONSULTATION.AI_REQUEST_FAILED";
}
