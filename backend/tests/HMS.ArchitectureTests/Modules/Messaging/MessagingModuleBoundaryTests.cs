using System.Reflection;
using FluentAssertions;
using HMS.Modules.Messaging.Endpoints;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.Messaging;

/// <summary>
/// Enforces the module-boundary rules from docs/Architecture.md §3–4 for
/// HMS.Modules.Messaging — mirrors HMS.ArchitectureTests.Modules.Notifications.
/// NotificationsModuleBoundaryTests exactly. Everything outside Contracts is internal, and
/// Contracts is the module's only public surface, with two deliberate, narrow exceptions
/// (see <see cref="AllowedPublicTypeNamePattern"/>): MessagingDbContext is public because
/// it's resolved by type from HMS.Api's Program.cs for the startup-time migration call, and
/// IConversationService is public for the same CS0051 reason as HMS.Modules.Identity's
/// IUserService (ConversationsController's public constructor takes it as a dependency).
/// </summary>
public class MessagingModuleBoundaryTests
{
    private static readonly Assembly MessagingAssembly = typeof(ConversationsController).Assembly;

    private const string AllowedPublicTypeNamePattern = "^(MessagingDbContext|IConversationService)$";

    [Theory]
    [InlineData("HMS.Modules.Messaging.Domain")]
    [InlineData("HMS.Modules.Messaging.Application")]
    [InlineData("HMS.Modules.Messaging.Infrastructure")]
    public void InternalLayers_ShouldNotExposePublicTypes(string layerNamespace)
    {
        var result = Types.InAssembly(MessagingAssembly)
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
        var result = Types.InAssembly(MessagingAssembly)
            .That()
            .ResideInNamespace("HMS.Modules.Messaging.Contracts")
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
