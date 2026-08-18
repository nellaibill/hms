using System.Reflection;
using FluentAssertions;
using HMS.Modules.Billing.Endpoints;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.Billing;

/// <summary>
/// Enforces the module-boundary rules from docs/Architecture.md §3–4 for
/// HMS.Modules.Billing — mirrors HMS.ArchitectureTests.Modules.IPD.IPDModuleBoundaryTests.
/// Everything outside Contracts is internal, and Contracts is the module's only public
/// surface, with the same two deliberate exceptions every other module has: IInvoiceService
/// is public because InvoicesController — which ASP.NET Core requires to be public with a
/// public constructor for controller discovery/DI activation — takes it as a constructor
/// dependency (a public constructor cannot have an internal parameter type, CS0051).
/// BillingDbContext is public because it's resolved by type from HMS.Api's Program.cs for
/// the startup-time migration call.
/// </summary>
public class BillingModuleBoundaryTests
{
    private static readonly Assembly BillingAssembly = typeof(InvoicesController).Assembly;

    private const string AllowedPublicTypeNamePattern = "^(BillingDbContext|IInvoiceService)$";

    [Theory]
    [InlineData("HMS.Modules.Billing.Domain")]
    [InlineData("HMS.Modules.Billing.Application")]
    [InlineData("HMS.Modules.Billing.Infrastructure")]
    public void InternalLayers_ShouldNotExposePublicTypes(string layerNamespace)
    {
        var result = Types.InAssembly(BillingAssembly)
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
        var result = Types.InAssembly(BillingAssembly)
            .That()
            .ResideInNamespace("HMS.Modules.Billing.Contracts")
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
