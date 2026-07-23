using HMS.Shared.Kernel;

namespace HMS.Modules.Roles.Contracts;

/// <summary>
/// Query parameters for GET /api/v1/roles.
/// </summary>
public class RoleListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}