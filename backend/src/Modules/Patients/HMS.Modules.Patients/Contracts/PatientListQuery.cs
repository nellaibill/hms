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

    /// <summary>Filters to patients registered (CreatedAt) within this inclusive range — added
    /// for Patient Reports, so a bounded date range doesn't have to walk every page of every
    /// patient ever registered just to filter client-side afterward.</summary>
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
