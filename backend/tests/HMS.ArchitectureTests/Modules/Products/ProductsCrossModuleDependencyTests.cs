using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.Products;

/// <summary>
/// Products is the first module with a real cross-module dependency on another module's
/// services (Masters' classification/unit lookups) rather than depending on nothing, so its
/// rule differs from HMS.ArchitectureTests.Modules.Identity.CrossModuleDependencyTests'
/// simpler blanket ban. Masters' own public seam intentionally lives in its Application
/// namespace (the I{Entity}Service interfaces — see MastersModuleBoundaryTests'
/// AllowedPublicTypeNamePattern), not in Contracts, so Products may depend on
/// HMS.Modules.Masters.Application and .Contracts, but never on its Domain or
/// Infrastructure — the two layers that are genuinely private to Masters.
/// </summary>
public class ProductsCrossModuleDependencyTests
{
    [Fact]
    public void Products_ShouldNotDependOnMastersInternals()
    {
        var productsAssembly = Assembly.Load("HMS.Modules.Products");

        var result = Types.InAssembly(productsAssembly)
            .Should()
            .NotHaveDependencyOnAny("HMS.Modules.Masters.Domain", "HMS.Modules.Masters.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Theory]
    [InlineData("HMS.Modules.Patients")]
    [InlineData("HMS.Modules.Appointments")]
    [InlineData("HMS.Modules.Staff")]
    [InlineData("HMS.Modules.Billing")]
    [InlineData("HMS.Modules.Notifications")]
    [InlineData("HMS.Modules.Masters")]
    public void OtherModules_ShouldNotDependOnProductsInternals(string otherModuleAssemblyName)
    {
        var otherModuleAssembly = Assembly.Load(otherModuleAssemblyName);

        var result = Types.InAssembly(otherModuleAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "HMS.Modules.Products.Domain",
                "HMS.Modules.Products.Application",
                "HMS.Modules.Products.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
