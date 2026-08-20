using System.Reflection;
using FluentAssertions;
using HMS.Modules.Pharmacy.Endpoints;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.Pharmacy;

/// <summary>
/// Enforces the module-boundary rules from docs/Architecture.md §3–4 for
/// HMS.Modules.Pharmacy — mirrors HMS.ArchitectureTests.Modules.IPD.IPDModuleBoundaryTests.
/// Everything outside Contracts is internal, and Contracts is the module's only public
/// surface, with a deliberate, narrow exception per use case: each I{UseCase}Service is
/// public because its {UseCase}Controller — which ASP.NET Core requires to be public with a
/// public constructor for controller discovery/DI activation — takes it as a constructor
/// dependency (a public constructor cannot have an internal parameter type, CS0051).
/// PharmacyDbContext is public because it's resolved by type from HMS.Api's Program.cs for
/// the startup-time migration call.
/// </summary>
public class PharmacyModuleBoundaryTests
{
    private static readonly Assembly PharmacyAssembly = typeof(StockReceiptsController).Assembly;

    private const string AllowedPublicTypeNamePattern =
        "^(PharmacyDbContext|IStockReceiptService|IDispenseService|IStockBalanceService|IStockLedgerService)$";

    [Theory]
    [InlineData("HMS.Modules.Pharmacy.Domain")]
    [InlineData("HMS.Modules.Pharmacy.Application")]
    [InlineData("HMS.Modules.Pharmacy.Infrastructure")]
    public void InternalLayers_ShouldNotExposePublicTypes(string layerNamespace)
    {
        var result = Types.InAssembly(PharmacyAssembly)
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
        var result = Types.InAssembly(PharmacyAssembly)
            .That()
            .ResideInNamespace("HMS.Modules.Pharmacy.Contracts")
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
