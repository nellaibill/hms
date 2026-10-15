using System.Reflection;
using FluentAssertions;
using HMS.Modules.Appointments.Endpoints;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.Appointments;

/// <summary>
/// Enforces the module-boundary rules from docs/Architecture.md §3–4 for
/// HMS.Modules.Appointments — mirrors CalendarModuleBoundaryTests. Everything outside Contracts
/// is internal, with the usual narrow exceptions: IAppointmentService (a constructor dependency
/// of the public AppointmentsController, CS0051), AppointmentsDbContext (resolved by type for
/// migrations) and AppointmentsModule (the DI entry point HMS.Api calls).
/// </summary>
public class AppointmentsModuleBoundaryTests
{
    private static readonly Assembly AppointmentsAssembly = typeof(AppointmentsController).Assembly;

    private const string AllowedPublicTypeNamePattern = "^(IAppointmentService|AppointmentsDbContext)$";

    [Theory]
    [InlineData("HMS.Modules.Appointments.Domain")]
    [InlineData("HMS.Modules.Appointments.Application")]
    [InlineData("HMS.Modules.Appointments.Infrastructure")]
    public void InternalLayers_ShouldNotExposePublicTypes(string layerNamespace)
    {
        var result = Types.InAssembly(AppointmentsAssembly)
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
        var result = Types.InAssembly(AppointmentsAssembly)
            .That()
            .ResideInNamespace("HMS.Modules.Appointments.Contracts")
            .Should()
            .BePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void Module_ShouldOnlyUseOtherModulesThroughTheirPublicSeams()
    {
        var result = Types.InAssembly(AppointmentsAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "HMS.Modules.Masters.Domain",
                "HMS.Modules.Masters.Infrastructure",
                "HMS.Modules.Patients.Domain",
                "HMS.Modules.Patients.Infrastructure",
                "HMS.Modules.ActivityLog.Domain",
                "HMS.Modules.ActivityLog.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    private static string FailureMessage(TestResult result) =>
        result.FailingTypeNames is null
            ? "Rule failed."
            : "Rule failed for: " + string.Join(", ", result.FailingTypeNames);
}
