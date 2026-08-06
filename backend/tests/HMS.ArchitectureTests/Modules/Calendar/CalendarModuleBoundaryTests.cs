using System.Reflection;
using FluentAssertions;
using HMS.Modules.Calendar.Endpoints;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.Calendar;

/// <summary>
/// Enforces the module-boundary rules from docs/Architecture.md §3–4 for
/// HMS.Modules.Calendar — mirrors HMS.ArchitectureTests.Modules.HR.HRModuleBoundaryTests.
/// Everything outside Contracts is internal, and Contracts is the module's only public
/// surface, with a deliberate, narrow exception (see <see cref="AllowedPublicTypeNamePattern"/>):
/// IEventService is public because EventsController — which ASP.NET Core requires to be
/// public with a public constructor for controller discovery/DI activation — takes it
/// as a constructor dependency (a public constructor cannot have an internal parameter
/// type, CS0051). CalendarDbContext is public because it's resolved by type from
/// HMS.Api's Program.cs for the startup-time migration call.
/// </summary>
public class CalendarModuleBoundaryTests
{
    private static readonly Assembly CalendarAssembly = typeof(EventsController).Assembly;

    private const string AllowedPublicTypeNamePattern = "^(IEventService|CalendarDbContext)$";

    [Theory]
    [InlineData("HMS.Modules.Calendar.Domain")]
    [InlineData("HMS.Modules.Calendar.Application")]
    [InlineData("HMS.Modules.Calendar.Infrastructure")]
    public void InternalLayers_ShouldNotExposePublicTypes(string layerNamespace)
    {
        var result = Types.InAssembly(CalendarAssembly)
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
        var result = Types.InAssembly(CalendarAssembly)
            .That()
            .ResideInNamespace("HMS.Modules.Calendar.Contracts")
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
