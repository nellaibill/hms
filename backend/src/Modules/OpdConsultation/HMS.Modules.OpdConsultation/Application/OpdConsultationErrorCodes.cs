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
}
