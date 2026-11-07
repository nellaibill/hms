using System.Security.Claims;
using FluentAssertions;
using HMS.Shared.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace HMS.UnitTests.Shared.Infrastructure;

public class AnyPermissionAuthorizationHandlerTests
{
    private static async Task<AuthorizationHandlerContext> HandleAsync(string[] callerPermissions, params string[] requiredAnyOf)
    {
        var claims = callerPermissions.Select(p => new Claim("Permission", p));
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        var context = new AuthorizationHandlerContext([new AnyPermissionRequirement(requiredAnyOf)], user, resource: null);
        await new AnyPermissionAuthorizationHandler().HandleAsync(context);
        return context;
    }

    [Fact]
    public async Task Succeeds_WhenTheCallerHasOnlyTheSecondOfTheListedPermissions()
    {
        var context = await HandleAsync(["patient-management.view"], "records-compliance.view", "patient-management.view");

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Succeeds_WhenTheCallerHasEveryListedPermission()
    {
        var context = await HandleAsync(["records-compliance.view", "patient-management.view"], "records-compliance.view", "patient-management.view");

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Fails_WhenTheCallerHasNoneOfTheListedPermissions()
    {
        var context = await HandleAsync(["finance-billing.view", "patient-management.edit"], "records-compliance.view", "patient-management.view");

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Fails_ForAClaimOfAnotherTypeWithAMatchingValue()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("Role", "records-compliance.view")], "Test"));
        var context = new AuthorizationHandlerContext([new AnyPermissionRequirement(["records-compliance.view"])], user, resource: null);

        await new AnyPermissionAuthorizationHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public void Requirement_RejectsAnEmptyList()
    {
        var act = () => new AnyPermissionRequirement([]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Attribute_YieldsOneAnyOfRequirementWithEveryKey()
    {
        var attribute = new RequireAnyPermissionAttribute("records-compliance.view", "patient-management.view");

        attribute.GetRequirements().Cast<AnyPermissionRequirement>().Single().PermissionKeys
            .Should().Equal("records-compliance.view", "patient-management.view");
    }
}
