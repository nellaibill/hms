using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.Messaging;

/// <summary>
/// Messaging is the third module with a real cross-module dependency on another module's
/// services (after Products→Masters and Notifications→Identity), so its rule mirrors
/// HMS.ArchitectureTests.Modules.Notifications.NotificationsCrossModuleDependencyTests'
/// exact shape. Notifications' own public seam intentionally lives in its Application
/// namespace (INotificationService — see NotificationsModuleBoundaryTests'
/// AllowedPublicTypeNamePattern), not in Contracts, so Messaging may depend on
/// HMS.Modules.Notifications.Application and .Contracts (used to raise the new-message
/// in-app alert — docs/DecisionLog.md ADR-034), but never on Notifications' Domain or
/// Infrastructure — the two layers that are genuinely private to Notifications.
/// </summary>
public class MessagingCrossModuleDependencyTests
{
    [Fact]
    public void Messaging_ShouldNotDependOnNotificationsInternals()
    {
        var messagingAssembly = Assembly.Load("HMS.Modules.Messaging");

        var result = Types.InAssembly(messagingAssembly)
            .Should()
            .NotHaveDependencyOnAny("HMS.Modules.Notifications.Domain", "HMS.Modules.Notifications.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Messaging_ShouldNotDependOnIdentityInternals()
    {
        // Messaging has no need to reach Identity directly (unlike Notifications, which
        // resolves recipient contact info) — it only ever carries opaque UserId Guids, so
        // this stays a full blanket ban, not the narrower Application-allowed exception
        // Notifications gets.
        var messagingAssembly = Assembly.Load("HMS.Modules.Messaging");

        var result = Types.InAssembly(messagingAssembly)
            .Should()
            .NotHaveDependencyOnAny("HMS.Modules.Identity.Domain", "HMS.Modules.Identity.Application", "HMS.Modules.Identity.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Theory]
    [InlineData("HMS.Modules.Patients")]
    [InlineData("HMS.Modules.Appointments")]
    [InlineData("HMS.Modules.Staff")]
    [InlineData("HMS.Modules.Billing")]
    [InlineData("HMS.Modules.Pharmacy")]
    [InlineData("HMS.Modules.Notifications")]
    public void OtherModules_ShouldNotDependOnMessagingInternals(string otherModuleAssemblyName)
    {
        var otherModuleAssembly = Assembly.Load(otherModuleAssemblyName);

        var result = Types.InAssembly(otherModuleAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "HMS.Modules.Messaging.Domain",
                "HMS.Modules.Messaging.Application",
                "HMS.Modules.Messaging.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
