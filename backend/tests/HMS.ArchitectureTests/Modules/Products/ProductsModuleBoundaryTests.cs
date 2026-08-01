using System.Reflection;
using FluentAssertions;
using HMS.Modules.Products.Endpoints;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.Products;

/// <summary>
/// Enforces the module-boundary rules from docs/Architecture.md §3–4 for HMS.Modules.Products
/// — mirrors HMS.ArchitectureTests.Modules.Masters.MastersModuleBoundaryTests. Everything
/// outside Contracts is internal, and Contracts is the module's only public surface, with a
/// deliberate, narrow exception per entity (see <see cref="AllowedPublicTypeNamePattern"/>):
/// each entity's I{Entity}Service is public because its {Entity}sController — which ASP.NET
/// Core requires to be public with a public constructor for controller discovery/DI
/// activation — takes it as a constructor dependency (a public constructor cannot have an
/// internal parameter type, CS0051). ProductsDbContext is public because it's resolved by
/// type from HMS.Api's Program.cs for the startup-time migration call.
/// </summary>
public class ProductsModuleBoundaryTests
{
    private static readonly Assembly ProductsAssembly = typeof(ProductsController).Assembly;

    private const string AllowedPublicTypeNamePattern =
        "^(ProductsDbContext" +
        "|IProductService|IProductBarcodeService|IProductBatchService|IProductPriceService" +
        "|IProductImageService|IProductAttributeService|IProductAttributeValueService|IProductTaxMappingService)$";

    [Theory]
    [InlineData("HMS.Modules.Products.Domain")]
    [InlineData("HMS.Modules.Products.Application")]
    [InlineData("HMS.Modules.Products.Infrastructure")]
    public void InternalLayers_ShouldNotExposePublicTypes(string layerNamespace)
    {
        var result = Types.InAssembly(ProductsAssembly)
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
        var result = Types.InAssembly(ProductsAssembly)
            .That()
            .ResideInNamespace("HMS.Modules.Products.Contracts")
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
