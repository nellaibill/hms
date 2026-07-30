using HMS.Modules.Identity.Contracts;

namespace HMS.Modules.Identity.Application;

public interface IPermissionService
{
    Task<IReadOnlyList<PermissionResponse>> GetAllAsync(
        CancellationToken cancellationToken);
}
