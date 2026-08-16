using Microsoft.AspNetCore.Authorization;

namespace HMS.Shared.Infrastructure;

/// <summary>
/// Satisfied when the caller's JWT carries a "Permission" claim matching
/// <see cref="PermissionKey"/> — HMS.Modules.Identity.Infrastructure.JwtTokenGenerator issues
/// one such claim per permission attached to the caller's role (via Role.RolePermissions),
/// resolved at login time. Mirrors the shape of every other claim-based check in this
/// codebase (e.g. HMS.Api.Configuration.JwtConfiguration's "Hospital"/"Platform" policies).
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permissionKey)
    {
        PermissionKey = permissionKey;
    }

    public string PermissionKey { get; }
}

/// <summary>
/// Apply alongside a bare <c>[Authorize]</c> (which selects <c>AuthorizationOptions.DefaultPolicy</c>
/// — the "Hospital" policy set up in HMS.Api.Configuration.JwtConfiguration) on any hospital-facing
/// action that should additionally require a specific permission — e.g.
/// <c>[Authorize] [RequirePermission("patient-management.create")]</c>. ASP.NET Core merges this
/// attribute's requirement into that same policy, so the caller must satisfy both: a valid
/// Hospital token AND the named permission. Using <see cref="IAuthorizationRequirementData"/>
/// (available since .NET 8) means no policy needs pre-registering per permission key in
/// JwtConfiguration.cs — the permission catalog can grow without touching host wiring.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : Attribute, IAuthorizationRequirementData
{
    private readonly string _permissionKey;

    public RequirePermissionAttribute(string permissionKey)
    {
        _permissionKey = permissionKey;
    }

    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return new PermissionRequirement(_permissionKey);
    }
}

/// <summary>
/// Registered once (singleton — holds no per-request state) in
/// HMS.Api.Configuration.JwtConfiguration alongside the "Hospital"/"Platform" policies.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var hasPermission = context.User.Claims.Any(c =>
            c.Type == "Permission" && c.Value == requirement.PermissionKey);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
