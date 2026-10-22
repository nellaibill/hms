using HMS.Modules.Identity.Application.Abstractions;
using HMS.Modules.Identity.Domain;

namespace HMS.Modules.Identity.Infrastructure.Seed;

/// <summary>
/// Keeps an existing "Super Admin" system role holding every permission in the catalog.
/// The catalog grows through migrations (each new module seeds its permission rows via
/// HasData), but a role's permission links only change when something re-assigns them —
/// so without this, a hospital provisioned before e.g. discharge-summary.* existed keeps a
/// Super Admin that gets 403 on that module's endpoints forever. Shared by both entry
/// points that can bring a database's catalog forward: <see cref="IdentityDataSeeder"/>
/// (legacy database, every startup) and <see cref="IdentityModule.SyncTenantSuperAdminPermissionsAsync"/>
/// (every hospital database, whenever TenantMigrationService migrates it). Never creates the
/// role — that is provisioning's/the seeder's job.
/// </summary>
internal sealed class SuperAdminPermissionSync
{
    public const string SuperAdminRoleName = "Super Admin";

    private readonly IPermissionRepository _permissionRepository;
    private readonly IRoleRepository _roleRepository;

    public SuperAdminPermissionSync(IPermissionRepository permissionRepository, IRoleRepository roleRepository)
    {
        _permissionRepository = permissionRepository;
        _roleRepository = roleRepository;
    }

    /// <returns>
    /// The Super Admin role (null when this database has none yet, e.g. mid-provisioning)
    /// and whether its permissions had to be re-synced and saved.
    /// </returns>
    public async Task<(Role? Role, bool Resynced)> SyncAsync(CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByNameAsync(SuperAdminRoleName, cancellationToken);
        if (role is null)
        {
            return (null, false);
        }

        var permissions = await _permissionRepository.GetAllAsync(cancellationToken);
        var permissionIds = permissions.Select(p => p.Id).ToList();

        var currentPermissionIds = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();
        if (currentPermissionIds.SetEquals(permissionIds))
        {
            return (role, false);
        }

        role.ReplacePermissions(permissionIds);
        await _roleRepository.SaveChangesAsync(cancellationToken);

        return (role, true);
    }
}
