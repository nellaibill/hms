using System.Reflection;
using FluentAssertions;
using HMS.Modules.OpdConsultation.Endpoints;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.OpdConsultation;

/// <summary>
/// Enforces the module-boundary rules from docs/Architecture.md §3–4 for
/// HMS.Modules.OpdConsultation — mirrors HMS.ArchitectureTests.Modules.DischargeSummary.
/// DischargeSummaryModuleBoundaryTests exactly. Everything outside Contracts is internal, and
/// Contracts is the module's only public surface, with two deliberate, narrow exceptions (see
/// <see cref="AllowedPublicTypeNamePattern"/>): OpdConsultationDbContext is public because it's
/// resolved by type from HMS.Api's Program.cs/TenantMigrationService for the startup/tenant
/// migration call, and IOpdConsultationService is public for the same CS0051 reason as
/// HMS.Modules.DischargeSummary's IDischargeSummaryService.
/// </summary>
public class OpdConsultationModuleBoundaryTests
{
    private static readonly Assembly OpdConsultationAssembly = typeof(OpdConsultationsController).Assembly;

    private const string AllowedPublicTypeNamePattern = "^(OpdConsultationDbContext|IOpdConsultationService)$";

    [Theory]
    [InlineData("HMS.Modules.OpdConsultation.Domain")]
    [InlineData("HMS.Modules.OpdConsultation.Application")]
    [InlineData("HMS.Modules.OpdConsultation.Infrastructure")]
    public void InternalLayers_ShouldNotExposePublicTypes(string layerNamespace)
    {
        var result = Types.InAssembly(OpdConsultationAssembly)
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
        var result = Types.InAssembly(OpdConsultationAssembly)
            .That()
            .ResideInNamespace("HMS.Modules.OpdConsultation.Contracts")
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
