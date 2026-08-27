using System.Reflection;
using FluentAssertions;
using HMS.Modules.HR.Endpoints;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.HR;

/// <summary>
/// Enforces the module-boundary rules from docs/Architecture.md §3–4 for HMS.Modules.HR
/// — mirrors HMS.ArchitectureTests.Modules.Masters.MastersModuleBoundaryTests. Everything
/// outside Contracts is internal, and Contracts is the module's only public surface, with a
/// deliberate, narrow exception (see <see cref="AllowedPublicTypeNamePattern"/>):
/// IShiftService/IShiftAssignmentService/IWeeklyRosterService/IStaffAvailabilityService/
/// IShiftSwapRequestService are public because their respective controllers — which ASP.NET
/// Core requires to be public with a public constructor for controller discovery/DI
/// activation — take them as constructor dependencies (a public constructor cannot have an
/// internal parameter type, CS0051). HRDbContext is public because it's resolved by type
/// from HMS.Api's Program.cs for the startup-time migration call. Department was
/// consolidated out of this module into Masters (see docs/DecisionLog.md) — this module now
/// only consumes Masters' public IDepartmentService, it doesn't define one.
/// </summary>
public class HRModuleBoundaryTests
{
    private static readonly Assembly HRAssembly = typeof(ShiftsController).Assembly;

    private const string AllowedPublicTypeNamePattern =
        "^(IShiftService|IShiftAssignmentService|IWeeklyRosterService|IStaffAvailabilityService|IShiftSwapRequestService|IEmployeeService|IAttendanceService|ILeaveTypeService|ILeaveRequestService|HRDbContext)$";

    [Theory]
    [InlineData("HMS.Modules.HR.Domain")]
    [InlineData("HMS.Modules.HR.Application")]
    [InlineData("HMS.Modules.HR.Infrastructure")]
    public void InternalLayers_ShouldNotExposePublicTypes(string layerNamespace)
    {
        var result = Types.InAssembly(HRAssembly)
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
        var result = Types.InAssembly(HRAssembly)
            .That()
            .ResideInNamespace("HMS.Modules.HR.Contracts")
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
