using System.Reflection;
using FluentAssertions;
using HMS.Modules.Radiology.Endpoints;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.Radiology;

/// <summary>
/// Same module-boundary rules as HMS.ArchitectureTests.Modules.Documents.DocumentsModuleBoundaryTests:
/// everything outside Contracts is internal. IRadiologyAiService is the one deliberate exception —
/// RadiologyAiController must be public with a public constructor for ASP.NET Core controller
/// discovery/DI activation, and a public constructor can't take an internal parameter type (CS0051).
/// RadiologyDbContext is the other: HMS.Api's TenantMigrationService resolves it by type for the
/// tenant migration call.
/// </summary>
public class RadiologyModuleBoundaryTests
{
    private static readonly Assembly RadiologyAssembly = typeof(RadiologyAiController).Assembly;

    private const string AllowedPublicTypeNamePattern = "^(IRadiologyAiService|RadiologyDbContext)$";

    [Theory]
    [InlineData("HMS.Modules.Radiology.Domain")]
    [InlineData("HMS.Modules.Radiology.Application")]
    [InlineData("HMS.Modules.Radiology.Infrastructure")]
    public void InternalLayers_ShouldNotExposePublicTypes(string layerNamespace)
    {
        var result = Types.InAssembly(RadiologyAssembly)
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
        var result = Types.InAssembly(RadiologyAssembly)
            .That()
            .ResideInNamespace("HMS.Modules.Radiology.Contracts")
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
