using System.Security.Claims;
using FluentAssertions;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace HMS.UnitTests.Shared.Infrastructure;

public class FeatureAuthorizationHandlerTests
{
    private static async Task<AuthorizationHandlerContext> HandleAsync(ITenantContext tenantContext, string requiredFeatureKey)
    {
        var requirement = new FeatureRequirement(requiredFeatureKey);
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(new ClaimsIdentity()), resource: null);
        await new FeatureAuthorizationHandler(tenantContext).HandleAsync(context);
        return context;
    }

    [Fact]
    public async Task Succeeds_WhenTenantHasTheRequiredFeatureEnabled()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetTenant(Guid.NewGuid(), "Host=localhost;Database=hms_tenant_apollo", enabledFeatures: ["hr", "calendar"]);

        var context = await HandleAsync(tenantContext, "hr");

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Fails_WhenTenantDoesNotHaveTheRequiredFeatureEnabled()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetTenant(Guid.NewGuid(), "Host=localhost;Database=hms_tenant_apollo", enabledFeatures: ["calendar"]);

        var context = await HandleAsync(tenantContext, "hr");

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Fails_WhenNoTenantWasResolvedForThisRequest()
    {
        // e.g. a Platform token, which never resolves a tenant at all.
        var tenantContext = new TenantContext();

        var context = await HandleAsync(tenantContext, "hr");

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Fails_DisabledFeatureIsRejectedEvenThoughEveryOtherFeatureIsEnabled()
    {
        // Live per-request re-resolution is the whole point (see FeatureAuthorizationHandler's
        // own doc comment) — this asserts the handler reads ITenantContext, not a stale claim.
        var tenantContext = new TenantContext();
        tenantContext.SetTenant(Guid.NewGuid(), "Host=localhost;Database=hms_tenant_apollo", enabledFeatures: FeatureCatalog.Mandatory);

        var context = await HandleAsync(tenantContext, "hr");

        context.HasSucceeded.Should().BeFalse();
    }
}
