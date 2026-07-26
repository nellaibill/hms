using HMS.Shared.Kernel;

namespace HMS.Modules.Patients.Contracts;

/// <summary>
/// Query parameters for GET /api/v1/patients — pagination/sort/search come from
/// <see cref="PagedRequest"/> (search matches Name, UHID, or Phone, per
/// docs/PatientRegistrationModule.md §8 — duplicate-confidence ranking is deferred).
/// </summary>
public class PatientListQuery : PagedRequest
{
}
