using System.Reflection;
using FluentAssertions;
using HMS.Modules.Masters.Endpoints;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.Masters;

/// <summary>
/// Enforces the module-boundary rules from docs/Architecture.md §3–4 for HMS.Modules.Masters
/// — mirrors HMS.ArchitectureTests.Modules.Identity.IdentityModuleBoundaryTests. Everything
/// outside Contracts is internal, and Contracts is the module's only public surface, with a
/// deliberate, narrow exception per entity (see <see cref="AllowedPublicTypeNamePattern"/>):
/// each entity's I{Entity}Service is public because its {Entity}sController — which ASP.NET
/// Core requires to be public with a public constructor for controller discovery/DI
/// activation — takes it as a constructor dependency (a public constructor cannot have an
/// internal parameter type, CS0051). MastersDbContext is public because it's resolved by
/// type from HMS.Api's Program.cs for the startup-time migration call.
/// </summary>
public class MastersModuleBoundaryTests
{
    private static readonly Assembly MastersAssembly = typeof(ProductCategoriesController).Assembly;

    private const string AllowedPublicTypeNamePattern =
        "^(MastersDbContext" +
        "|IProductCategoryService|IProductSubCategoryService|IProductGroupService" +
        "|IBrandService|IManufacturerService" +
        "|IUnitOfMeasureService|IUnitConversionService|ITaxService" +
        "|IWarehouseService|IStorageLocationService" +
        "|ISupplierService|ICustomerService" +
        "|ICurrencyService|IPaymentTermService|IPaymentMethodService" +
        "|IStockAdjustmentReasonService)$";

    [Theory]
    [InlineData("HMS.Modules.Masters.Domain")]
    [InlineData("HMS.Modules.Masters.Application")]
    [InlineData("HMS.Modules.Masters.Infrastructure")]
    public void InternalLayers_ShouldNotExposePublicTypes(string layerNamespace)
    {
        var result = Types.InAssembly(MastersAssembly)
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
        var result = Types.InAssembly(MastersAssembly)
            .That()
            .ResideInNamespace("HMS.Modules.Masters.Contracts")
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
