using HMS.Shared.Kernel;

namespace HMS.Modules.Patients.Contracts;

/// <summary>Query shape for the cross-patient visits list (reporting) — unlike
/// PatientVisitsController's per-patient GET, this spans every patient, so it's paged and
/// date-range-filterable on CreatedAt rather than returning everything at once.</summary>
public class PatientVisitListQuery : PagedRequest
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
