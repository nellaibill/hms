using System.Reflection;
using FluentAssertions;
using HMS.Modules.ActivityLog.Endpoints;
using HMS.Shared.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace HMS.UnitTests.Modules.ActivityLog.Endpoints;

// ADR-150: the Activity Log (every user's actions) is the hospital Super Admin's alone — a
// doctor's or receptionist's role holding identity-administration.view is no longer enough.
public class ActivityLogsControllerAuthorizationTests
{
    [Fact]
    public void EveryActivityLogRead_RequiresTheSuperAdminPolicy()
    {
        var actions = typeof(ActivityLogsController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttributes<HttpGetAttribute>().Any())
            .ToList();

        actions.Should().NotBeEmpty();
        actions.Should().AllSatisfy(action =>
            action.GetCustomAttributes<AuthorizeAttribute>().Should().Contain(a => a.Policy == HospitalPolicies.SuperAdmin));
    }
}
