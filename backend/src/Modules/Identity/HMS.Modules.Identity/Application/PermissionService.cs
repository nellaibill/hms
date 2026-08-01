using HMS.Modules.Identity.Application.Abstractions;
using HMS.Modules.Identity.Application.Mapping;
using HMS.Modules.Identity.Contracts;

namespace HMS.Modules.Identity.Application;

internal sealed class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;

    public PermissionService(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    public async Task<IReadOnlyList<PermissionResponse>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var permissions =
            await _permissionRepository.GetAllAsync(cancellationToken);

        return permissions
            .Select(p => p.ToResponse())
            .ToList();
    }
}
