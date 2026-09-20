using System.Reflection;
using FluentAssertions;
using HMS.Modules.ActivityLog.Application;
using HMS.Modules.ActivityLog.Endpoints;
using Microsoft.AspNetCore.Mvc.Routing;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.ActivityLog;

/// <summary>
/// Module-boundary rules (docs/Architecture.md §3–4) for HMS.Modules.ActivityLog, plus the
/// audit-integrity rules: the HTTP surface is read-only and the write seam has no
/// update/delete.
/// </summary>
public class ActivityLogModuleBoundaryTests
{
    private static readonly Assembly ActivityLogAssembly = typeof(ActivityLogsController).Assembly;

    // ActivityLogDbContext: resolved by type from HMS.Api for the migration call.
    // IActivityLogService / IActivityLogQueryService: the module's public seams (CS0051 —
    // the controller's public constructor and other modules' services take them).
    private const string AllowedPublicTypeNamePattern = "^(ActivityLogDbContext|IActivityLogService|IActivityLogQueryService)$";

    [Theory]
    [InlineData("HMS.Modules.ActivityLog.Domain")]
    [InlineData("HMS.Modules.ActivityLog.Application")]
    [InlineData("HMS.Modules.ActivityLog.Infrastructure")]
    public void InternalLayers_ShouldNotExposePublicTypes(string layerNamespace)
    {
        var result = Types.InAssembly(ActivityLogAssembly)
            .That()
            .ResideInNamespaceStartingWith(layerNamespace)
            .And()
            .DoNotHaveNameMatching(AllowedPublicTypeNamePattern)
            .Should()
            .NotBePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void Contracts_ShouldBePublic()
    {
        var result = Types.InAssembly(ActivityLogAssembly)
            .That()
            .ResideInNamespace("HMS.Modules.ActivityLog.Contracts")
            .Should()
            .BePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Theory]
    [InlineData("HMS.Modules.Patients")]
    [InlineData("HMS.Modules.Identity")]
    [InlineData("HMS.Modules.Billing")]
    public void OtherModules_ShouldNotDependOnActivityLogInternals(string otherModuleAssemblyName)
    {
        var otherModuleAssembly = Assembly.Load(otherModuleAssemblyName);

        // Application is allowed (IActivityLogService lives there, like INotificationService);
        // Domain and Infrastructure are private.
        var result = Types.InAssembly(otherModuleAssembly)
            .Should()
            .NotHaveDependencyOnAny("HMS.Modules.ActivityLog.Domain", "HMS.Modules.ActivityLog.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void ActivityLog_ShouldNotDependOnAnyOtherModule()
    {
        var result = Types.InAssembly(ActivityLogAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "HMS.Modules.Identity", "HMS.Modules.Patients", "HMS.Modules.Billing", "HMS.Modules.Masters",
                "HMS.Modules.Notifications", "HMS.Modules.Messaging")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void Controller_ShouldExposeOnlyReadActions()
    {
        var actions = typeof(ActivityLogsController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        actions.Should().NotBeEmpty();
        foreach (var action in actions)
        {
            var verbs = action.GetCustomAttributes<HttpMethodAttribute>().SelectMany(a => a.HttpMethods).ToList();
            verbs.Should().OnlyContain(v => v == "GET", $"{action.Name} must be read-only");
        }
    }

    [Fact]
    public void ServiceInterfaces_ShouldExposeNoUpdateOrDeleteMethods()
    {
        var methods = typeof(IActivityLogService).GetMethods()
            .Concat(typeof(IActivityLogQueryService).GetMethods())
            .Select(m => m.Name);

        methods.Should().NotContain(n => n.Contains("Update") || n.Contains("Delete") || n.Contains("Remove"));
    }

    private static string FailureMessage(TestResult result) =>
        result.FailingTypeNames is null
            ? "Rule failed."
            : "Rule failed for: " + string.Join(", ", result.FailingTypeNames);
}
