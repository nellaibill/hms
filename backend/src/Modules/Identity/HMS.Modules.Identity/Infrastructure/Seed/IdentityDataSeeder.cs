using HMS.Modules.Identity.Application.Abstractions;
using HMS.Modules.Identity.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HMS.Modules.Identity.Infrastructure.Seed;

/// <summary>
/// Idempotent startup data seeder: ensures the "Super Admin" system role (with every
/// permission assigned) and a default Super Admin user exist. Runs once per startup,
/// after <c>IdentityDbContext.Database.Migrate()</c> has applied every pending
/// migration — see Program.cs. Reuses the same repositories, domain factory methods
/// (<see cref="Role.Create"/>, <see cref="User.Create"/>) and <see cref="IPasswordHasher"/>
/// that the ordinary Roles/Users API surface uses; this class only orchestrates them in
/// a different (non-HTTP) entry point, the same way RoleService.CreateAsync's own
/// comment already anticipated: "Public API never creates system roles; those are
/// seeded/provisioned internally."
/// </summary>
internal sealed class IdentityDataSeeder
{
    private const string SuperAdminRoleName = "Super Admin";

    private readonly IPermissionRepository _permissionRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SuperAdminSeedOptions _options;
    private readonly ILogger<IdentityDataSeeder> _logger;

    public IdentityDataSeeder(
        IPermissionRepository permissionRepository,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IOptions<SuperAdminSeedOptions> options,
        ILogger<IdentityDataSeeder> logger)
    {
        _permissionRepository = permissionRepository;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var superAdminRole = await EnsureSuperAdminRoleAsync(cancellationToken);
        await EnsureSuperAdminUserAsync(superAdminRole, cancellationToken);
    }

    /// <summary>
    /// Creates the "Super Admin" role with every currently-seeded permission attached,
    /// if it does not already exist. Permission *records* themselves are not created
    /// here — they already exist by this point via PermissionConfiguration.HasData,
    /// applied as part of the migration this seeder runs after — this only reads and
    /// assigns them. Re-syncs an already-existing role's permissions on every startup
    /// too (HMS Security Hardening Phase B): the permission catalog can grow after the
    /// role was first created (e.g. new module added), and without this an existing
    /// Super Admin would silently miss newly-seeded permissions — a self-inflicted
    /// lockout risk once actions start being gated with [RequirePermission].
    /// </summary>
    private async Task<Role> EnsureSuperAdminRoleAsync(CancellationToken cancellationToken)
    {
        var permissions = await _permissionRepository.GetAllAsync(cancellationToken);
        var permissionIds = permissions.Select(p => p.Id).ToList();

        var existingRole = await _roleRepository.GetByNameAsync(SuperAdminRoleName, cancellationToken);
        if (existingRole is not null)
        {
            var currentPermissionIds = existingRole.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();
            if (!currentPermissionIds.SetEquals(permissionIds))
            {
                existingRole.ReplacePermissions(permissionIds);
                await _roleRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Re-synced '{RoleName}' role to {PermissionCount} permission(s)",
                    SuperAdminRoleName,
                    permissionIds.Count);
            }

            return existingRole;
        }

        var role = Role.Create(
            SuperAdminRoleName,
            "Full system access with every permission assigned. Created automatically on first startup.",
            isSystemRole: true,
            displayOrder: 0,
            createdBy: null);

        role.ReplacePermissions(permissionIds);

        await _roleRepository.AddAsync(role, cancellationToken);
        await _roleRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Seeded '{RoleName}' role with {PermissionCount} permission(s)",
            SuperAdminRoleName,
            permissionIds.Count);

        return role;
    }

    /// <summary>
    /// Creates the default Super Admin user, if it does not already exist. Skipped
    /// (with a warning, not a startup failure) when SuperAdminSeed configuration is
    /// incomplete — e.g. a deployment that hasn't set the section yet should still
    /// start up and serve the rest of the API.
    /// </summary>
    private async Task EnsureSuperAdminUserAsync(Role superAdminRole, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Username) ||
            string.IsNullOrWhiteSpace(_options.FirstName) ||
            string.IsNullOrWhiteSpace(_options.LastName) ||
            string.IsNullOrWhiteSpace(_options.Email) ||
            string.IsNullOrWhiteSpace(_options.Password))
        {
            _logger.LogWarning(
                "Skipped Super Admin user seeding: 'SuperAdminSeed' configuration is incomplete (Username, FirstName, LastName, Email and Password are required).");
            return;
        }

        var existingUser = await _userRepository.GetByUsernameAsync(_options.Username, cancellationToken);
        if (existingUser is not null)
        {
            return;
        }

        var user = User.Create(
            _options.Username,
            _options.FirstName,
            _options.LastName,
            _options.Email,
            _options.PhoneNumber,
            superAdminRole.Id,
            createdBy: null);

        user.SetPasswordHash(_passwordHasher.HashPassword(_options.Password), updatedBy: null);

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded Super Admin user '{Username}'", user.Username);
    }
}
