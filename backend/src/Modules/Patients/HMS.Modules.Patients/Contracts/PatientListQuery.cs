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
}
