using HMS.Modules.Identity.Domain;

namespace HMS.Modules.Identity.Application.Abstractions;

/// <summary>
/// Provides read access to the system permission catalog.
/// </summary>
internal interface IPermissionRepository
{
    Task<IReadOnlyList<Permission>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Permission>> GetActiveByKeysAsync(
        IEnumerable<string> permissionKeys,
        CancellationToken cancellationToken);
}