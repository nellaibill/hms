using System.Reflection;
using FluentAssertions;
using HMS.Modules.DischargeSummary.Endpoints;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.DischargeSummary;

/// <summary>
/// Enforces the module-boundary rules from docs/Architecture.md §3–4 for
/// HMS.Modules.DischargeSummary — mirrors HMS.ArchitectureTests.Modules.Messaging.
/// MessagingModuleBoundaryTests exactly. Everything outside Contracts is internal, and
/// Contracts is the module's only public surface, with two deliberate, narrow exceptions
/// (see <see cref="AllowedPublicTypeNamePattern"/>): DischargeSummaryDbContext is public
/// because it's resolved by type from HMS.Api's Program.cs/TenantMigrationService for the
/// startup/tenant migration call, and IDischargeSummaryService is public for the same
/// CS0051 reason as HMS.Modules.IPD's IAdmissionService (DischargeSummariesController's
/// public constructor takes it as a dependency).
/// </summary>
public class DischargeSummaryModuleBoundaryTests
{
    private static readonly Assembly DischargeSummaryAssembly = typeof(DischargeSummariesController).Assembly;

    private const string AllowedPublicTypeNamePattern = "^(DischargeSummaryDbContext|IDischargeSummaryService)$";

    [Theory]
    [InlineData("HMS.Modules.DischargeSummary.Domain")]
    [InlineData("HMS.Modules.DischargeSummary.Application")]
    [InlineData("HMS.Modules.DischargeSummary.Infrastructure")]
    public void InternalLayers_ShouldNotExposePublicTypes(string layerNamespace)
    {
        var result = Types.InAssembly(DischargeSummaryAssembly)
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
        var result = Types.InAssembly(DischargeSummaryAssembly)
            .That()
            .ResideInNamespace("HMS.Modules.DischargeSummary.Contracts")
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
