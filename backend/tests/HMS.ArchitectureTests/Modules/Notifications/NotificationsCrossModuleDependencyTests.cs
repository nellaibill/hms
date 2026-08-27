using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.Notifications;

/// <summary>
/// Notifications is the second module (after HMS.ArchitectureTests.Modules.Products.
/// ProductsCrossModuleDependencyTests' Products→Masters case) with a real cross-module
/// dependency on another module's services rather than depending on nothing, so its rule
/// differs from HMS.ArchitectureTests.Modules.Identity.CrossModuleDependencyTests' simpler
/// blanket ban. Identity's own public seam intentionally lives in its Application namespace
/// (IUserService — see IdentityModuleBoundaryTests' AllowedPublicTypeNamePattern), not in
/// Contracts, so Notifications may depend on HMS.Modules.Identity.Application and .Contracts
/// (used to resolve a recipient's email/phone number for the background delivery pipeline —
/// docs/DecisionLog.md ADR-032), but never on Identity's Domain or Infrastructure — the two
/// layers that are genuinely private to Identity.
///
/// HMS.Modules.Messaging is excluded from <see cref="OtherModules_ShouldNotDependOnNotificationsInternals"/>'s
/// blanket ban for the same reason Notifications is excluded from Identity's: it legitimately
/// depends on Notifications' public INotificationService (the new-message alert —
/// docs/DecisionLog.md ADR-034), covered instead by its own
/// HMS.ArchitectureTests.Modules.Messaging.MessagingCrossModuleDependencyTests.
/// </summary>
public class NotificationsCrossModuleDependencyTests
{
    [Fact]
    public void Notifications_ShouldNotDependOnIdentityInternals()
    {
        var notificationsAssembly = Assembly.Load("HMS.Modules.Notifications");

        var result = Types.InAssembly(notificationsAssembly)
            .Should()
            .NotHaveDependencyOnAny("HMS.Modules.Identity.Domain", "HMS.Modules.Identity.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Theory]
    [InlineData("HMS.Modules.Patients")]
    [InlineData("HMS.Modules.Appointments")]
    [InlineData("HMS.Modules.Staff")]
    [InlineData("HMS.Modules.Billing")]
    [InlineData("HMS.Modules.Pharmacy")]
    public void OtherModules_ShouldNotDependOnNotificationsInternals(string otherModuleAssemblyName)
    {
        var otherModuleAssembly = Assembly.Load(otherModuleAssemblyName);

        var result = Types.InAssembly(otherModuleAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "HMS.Modules.Notifications.Domain",
                "HMS.Modules.Notifications.Application",
                "HMS.Modules.Notifications.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
