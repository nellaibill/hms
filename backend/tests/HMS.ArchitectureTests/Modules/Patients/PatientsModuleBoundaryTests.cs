using System.Reflection;
using FluentAssertions;
using HMS.Modules.Patients.Endpoints;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.Patients;

/// <summary>
/// Enforces the module-boundary rules from docs/Architecture.md §3–4 for
/// HMS.Modules.Patients — mirrors HMS.ArchitectureTests.Modules.Calendar.CalendarModuleBoundaryTests.
/// Everything outside Contracts is internal, and Contracts is the module's only public
/// surface, with a deliberate, narrow exception (see <see cref="AllowedPublicTypeNamePattern"/>):
/// IPatientService is public because PatientsController — which ASP.NET Core requires to be
/// public with a public constructor for controller discovery/DI activation — takes it as a
/// constructor dependency (a public constructor cannot have an internal parameter type,
/// CS0051). PatientsDbContext is public because it's resolved by type from HMS.Api's
/// Program.cs for the startup-time migration call.
/// </summary>
public class PatientsModuleBoundaryTests
{
    private static readonly Assembly PatientsAssembly = typeof(PatientsController).Assembly;

    private const string AllowedPublicTypeNamePattern = "^(IPatientService|PatientsDbContext)$";

    [Theory]
    [InlineData("HMS.Modules.Patients.Domain")]
    [InlineData("HMS.Modules.Patients.Application")]
    [InlineData("HMS.Modules.Patients.Infrastructure")]
    public void InternalLayers_ShouldNotExposePublicTypes(string layerNamespace)
    {
        var result = Types.InAssembly(PatientsAssembly)
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
        var result = Types.InAssembly(PatientsAssembly)
            .That()
            .ResideInNamespace("HMS.Modules.Patients.Contracts")
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
