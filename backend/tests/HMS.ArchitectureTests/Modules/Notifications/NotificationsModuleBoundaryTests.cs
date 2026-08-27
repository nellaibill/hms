using System.Reflection;
using FluentAssertions;
using HMS.Modules.Notifications.Endpoints;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.Notifications;

/// <summary>
/// Enforces the module-boundary rules from docs/Architecture.md §3–4 for
/// HMS.Modules.Notifications — mirrors HMS.ArchitectureTests.Modules.Calendar.
/// CalendarModuleBoundaryTests. Everything outside Contracts is internal, and Contracts is
/// the module's only public surface, with two deliberate, narrow exceptions (see
/// <see cref="AllowedPublicTypeNamePattern"/>): NotificationsDbContext is public because
/// it's resolved by type from HMS.Api's Program.cs for the startup-time migration call, and
/// INotificationService is public for the same CS0051 reason as HMS.Modules.Identity's
/// IUserService (NotificationsController's public constructor takes it as a dependency) —
/// it's also this module's cross-module call seam, per docs/DecisionLog.md ADR-029.
/// </summary>
public class NotificationsModuleBoundaryTests
{
    private static readonly Assembly NotificationsAssembly = typeof(NotificationsController).Assembly;

    private const string AllowedPublicTypeNamePattern = "^(NotificationsDbContext|INotificationService)$";

    [Theory]
    [InlineData("HMS.Modules.Notifications.Domain")]
    [InlineData("HMS.Modules.Notifications.Application")]
    [InlineData("HMS.Modules.Notifications.Infrastructure")]
    public void InternalLayers_ShouldNotExposePublicTypes(string layerNamespace)
    {
        var result = Types.InAssembly(NotificationsAssembly)
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
        var result = Types.InAssembly(NotificationsAssembly)
            .That()
            .ResideInNamespace("HMS.Modules.Notifications.Contracts")
            .Should()
            .BePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    private static string FailureMessage(TestResult result) =>
        result.FailingTypeNames is null
            ? "Rule failed."
            : "Rule failed for: " + string.Join(", ", result.FailingTypeNames);
}
