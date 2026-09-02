using System.Reflection;
using FluentAssertions;
using HMS.Modules.Laboratory.Endpoints;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.Laboratory;

/// <summary>
/// Enforces the module-boundary rules from docs/Architecture.md §3-4 for
/// HMS.Modules.Laboratory — mirrors HMS.ArchitectureTests.Modules.Billing.BillingModuleBoundaryTests.
/// Everything outside Contracts is internal, and Contracts is the module's only public
/// surface, with the same two deliberate exceptions every other module has: ILabOrderService
/// is public because LabOrdersController — which ASP.NET Core requires to be public with a
/// public constructor for controller discovery/DI activation — takes it as a constructor
/// dependency (a public constructor cannot have an internal parameter type, CS0051), and it's
/// also the cross-module seam Billing's InvoiceService calls into. LaboratoryDbContext is
/// public because it's resolved by type from HMS.Api's Program.cs/TenantMigrationService for
/// the startup-time/tenant-migration call — same as BillingDbContext.
/// </summary>
public class LaboratoryModuleBoundaryTests
{
    private static readonly Assembly LaboratoryAssembly = typeof(LabOrdersController).Assembly;

    private const string AllowedPublicTypeNamePattern = "^(LaboratoryDbContext|ILabOrderService)$";

    [Theory]
    [InlineData("HMS.Modules.Laboratory.Domain")]
    [InlineData("HMS.Modules.Laboratory.Application")]
    [InlineData("HMS.Modules.Laboratory.Infrastructure")]
    public void InternalLayers_ShouldNotExposePublicTypes(string layerNamespace)
    {
        var result = Types.InAssembly(LaboratoryAssembly)
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
        var result = Types.InAssembly(LaboratoryAssembly)
            .That()
            .ResideInNamespace("HMS.Modules.Laboratory.Contracts")
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
