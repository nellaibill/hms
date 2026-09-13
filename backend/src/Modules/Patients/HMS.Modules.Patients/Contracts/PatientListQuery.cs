using HMS.Shared.Kernel;

namespace HMS.Modules.Patients.Contracts;

/// <summary>
/// Query parameters for GET /api/v1/patients — pagination/sort come from
/// <see cref="PagedRequest"/>. <see cref="PagedRequest.Search"/> is a single free-text term
/// matched against Name/UHID/Phone together; the properties below are dedicated per-field
/// filters, AND'd together with each other and with Search when more than one is present.
/// </summary>
public class PatientListQuery : PagedRequest
{
    public string? Name { get; set; }
    public int? Age { get; set; }
    public string? Uhid { get; set; }
    public string? Phone { get; set; }

    /// <summary>When true, narrows the list to patients still flagged with placeholder data
    /// (e.g. from bulk import) — see Patient.RequiresDataVerification.</summary>
    public bool? RequiresDataVerification { get; set; }

    /// <summary>When true, narrows the list to patients with at least one PatientVisit
    /// (patients.patient_visits) created or last updated today (UTC) — used by OPD Billing's
    /// "pick a patient" screen to surface today's visits before any search is entered.</summary>
    public bool? RegisteredToday { get; set; }

    /// <summary>Filters to patients who were either registered, or had at least one visit,
    /// within this inclusive range — an "activity" scope, not pure registration date, so
    /// Patient Reports' New-vs-Returning split is meaningful (see PatientReportSummaryResponse's
    /// own doc comment). Added for Patient Reports; the plain Patients list/Enquiry page never
    /// sends these, so existing callers are unaffected.</summary>
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    public Gender? Gender { get; set; }
    public BloodGroup? BloodGroup { get; set; }

    /// <summary>Narrows to patients with at least one visit whose consultation lines name this
    /// department — Department lives on PatientVisitConsultation, not Patient itself, so this
    /// is an EXISTS filter, not a direct column match.</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>When true, GetPagedAsync additionally resolves each patient's most recent visit
    /// (Department/Consultant/AppointmentTime) onto the response — see PatientResponse's
    /// LastVisit* fields. Opt-in (defaults false/unset) so the plain Patients List/Enquiry pages
    /// don't pay for a lookup they don't use; set by OPD Billing's "pick a patient" screen,
    /// which shows this alongside each row.</summary>
    public bool? IncludeLastVisit { get; set; }
}
