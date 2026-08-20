using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.Pharmacy;

/// <summary>
/// Pharmacy has real cross-module dependencies on two other modules' public seams — Products'
/// (Product/ProductBatch existence) and Patients' (PatientId existence) — rather than
/// depending on nothing, so its rule mirrors
/// HMS.ArchitectureTests.Modules.Products.ProductsCrossModuleDependencyTests rather than
/// Identity's simpler blanket ban. Products' and Patients' own public seams intentionally
/// live in their Application namespace (the I{Entity}Service interfaces), not in Contracts,
/// so Pharmacy may depend on HMS.Modules.Products.Application/.Contracts and
/// HMS.Modules.Patients.Application/.Contracts, but never on either module's Domain or
/// Infrastructure — the two layers that are genuinely private to each.
/// </summary>
public class PharmacyCrossModuleDependencyTests
{
    [Fact]
    public void Pharmacy_ShouldNotDependOnProductsInternals()
    {
        var pharmacyAssembly = Assembly.Load("HMS.Modules.Pharmacy");

        var result = Types.InAssembly(pharmacyAssembly)
            .Should()
            .NotHaveDependencyOnAny("HMS.Modules.Products.Domain", "HMS.Modules.Products.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Pharmacy_ShouldNotDependOnPatientsInternals()
    {
        var pharmacyAssembly = Assembly.Load("HMS.Modules.Pharmacy");

        var result = Types.InAssembly(pharmacyAssembly)
            .Should()
            .NotHaveDependencyOnAny("HMS.Modules.Patients.Domain", "HMS.Modules.Patients.Infrastructure")
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
    [InlineData("HMS.Modules.Products")]
    [InlineData("HMS.Modules.IPD")]
    public void OtherModules_ShouldNotDependOnPharmacyInternals(string otherModuleAssemblyName)
    {
        var otherModuleAssembly = Assembly.Load(otherModuleAssemblyName);

        var result = Types.InAssembly(otherModuleAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "HMS.Modules.Pharmacy.Domain",
                "HMS.Modules.Pharmacy.Application",
                "HMS.Modules.Pharmacy.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
