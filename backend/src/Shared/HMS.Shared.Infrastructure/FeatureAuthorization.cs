using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;

namespace HMS.Shared.Infrastructure;

/// <summary>
/// Satisfied when the current tenant has FeatureCatalog key <see cref="FeatureKey"/> enabled
/// — Tenant Feature/Module Management's enforcement point. Deliberately NOT a JWT-claim
/// check like <see cref="PermissionRequirement"/>: a JWT claim is a snapshot taken at login,
/// so a Platform Admin disabling a feature wouldn't take effect until the affected users'
/// tokens expire or they re-login. FeatureAuthorizationHandler instead reads
/// ITenantContext.EnabledFeatures, which TenantResolutionMiddleware re-resolves from
/// platform.tenant_features on every single request (the same mechanism already used for
/// EnabledModules/tenant-active checks) — so a disabled feature is rejected on the very next
/// API call, with no re-login wait. The hospital JWT does also carry a "Feature" claim
/// (JwtTokenGenerator), but that is used only for frontend nav/UI gating convenience and is
/// never consulted here.
/// </summary>
public sealed class FeatureRequirement : IAuthorizationRequirement
{
    public FeatureRequirement(string featureKey)
    {
        FeatureKey = featureKey;
    }

    public string FeatureKey { get; }
}

/// <summary>
/// Apply alongside a bare <c>[Authorize]</c> on any hospital-facing action belonging to an
/// optional module — e.g. <c>[Authorize] [RequireFeature("hr")]</c>. Combine with
/// <c>[RequirePermission("...")]</c> where both a feature and an RBAC permission gate the
/// same action; ASP.NET Core merges every requirement attached to a policy, so the caller
/// must satisfy all of them.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireFeatureAttribute : Attribute, IAuthorizationRequirementData
{
    private readonly string _featureKey;

    public RequireFeatureAttribute(string featureKey)
    {
        _featureKey = featureKey;
    }

    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return new FeatureRequirement(_featureKey);
    }
}

/// <summary>
/// Registered Scoped (not Singleton like PermissionAuthorizationHandler) in
/// HMS.Api.Configuration.JwtConfiguration, because it depends on the request-scoped
/// ITenantContext — ASP.NET Core's authorization pipeline resolves IAuthorizationHandler
/// instances from the current request's service provider on every check, so a Scoped
/// registration here is resolved correctly per request.
/// </summary>
public sealed class FeatureAuthorizationHandler : AuthorizationHandler<FeatureRequirement>
{
    private readonly ITenantContext _tenantContext;

    public FeatureAuthorizationHandler(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, FeatureRequirement requirement)
    {
        // Null means no tenant was resolved for this request (e.g. a Platform token) — a
        // feature requirement can never be satisfied without a resolved hospital tenant.
        if (_tenantContext.EnabledFeatures?.Contains(requirement.FeatureKey) == true)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
