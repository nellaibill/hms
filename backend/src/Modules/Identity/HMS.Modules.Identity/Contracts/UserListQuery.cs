using HMS.Shared.Kernel;

namespace HMS.Modules.Identity.Contracts;

/// <summary>
/// Query parameters for GET /api/v1/users — pagination/sort/search come from
/// <see cref="PagedRequest"/>; <see cref="IsActive"/> is this endpoint's own filter.
/// "Search" (docs/modules/Identity/Users.md) is implemented as this endpoint's
/// <c>search</c> parameter rather than a separate route, per docs/ApiStandards.md §6.
/// </summary>
public class UserListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
