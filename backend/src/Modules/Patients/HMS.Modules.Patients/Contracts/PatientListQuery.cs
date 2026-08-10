using HMS.Shared.Kernel;

namespace HMS.Modules.Patients.Contracts;

/// <summary>
/// Query parameters for GET /api/v1/patients — pagination/sort come from
/// <see cref="PagedRequest"/>. <see cref="PagedRequest.Search"/> is a single free-text term
/// matched against Name/UHID/Phone together; the four properties below are the dedicated
/// per-field filters the UI's separate search boxes actually send (AND'd together with each
/// other and with Search when more than one is present) — see PatientRepository.GetPagedAsync.
/// Duplicate-confidence ranking is deferred (docs/PatientRegistrationModule.md §8).
/// </summary>
public class PatientListQuery : PagedRequest
{
    public string? Name { get; set; }
    public int? Age { get; set; }
    public string? Uhid { get; set; }
    public string? Phone { get; set; }
}
