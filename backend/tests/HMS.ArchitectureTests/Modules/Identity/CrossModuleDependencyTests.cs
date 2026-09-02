using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.Identity;

/// <summary>
/// No module may depend on Identity's internals — only its Contracts, per
/// docs/Architecture.md §3. Most modules have no legitimate reason to touch Identity at all,
/// so they get this blanket ban including Application; HMS.Modules.Notifications is the one
/// exception (it legitimately depends on Identity's public IUserService, which lives in
/// Identity's Application namespace, to resolve a recipient's email/phone for the delivery
/// pipeline — docs/DecisionLog.md ADR-032), so it's excluded here and covered instead by its
/// own HMS.ArchitectureTests.Modules.Notifications.NotificationsCrossModuleDependencyTests,
/// which mirrors HMS.ArchitectureTests.Modules.Products.ProductsCrossModuleDependencyTests'
/// Application-allowed-but-not-Domain/Infrastructure shape.
/// </summary>
public class CrossModuleDependencyTests
{
    [Theory]
    [InlineData("HMS.Modules.Patients")]
    [InlineData("HMS.Modules.Appointments")]
    [InlineData("HMS.Modules.Staff")]
    [InlineData("HMS.Modules.Billing")]
    [InlineData("HMS.Modules.Laboratory")]
    [InlineData("HMS.Modules.Messaging")]
    [InlineData("HMS.Modules.Pharmacy")]
    public void OtherModules_ShouldNotDependOnIdentityInternals(string otherModuleAssemblyName)
    {
        var otherModuleAssembly = Assembly.Load(otherModuleAssemblyName);

        var result = Types.InAssembly(otherModuleAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "HMS.Modules.Identity.Domain",
                "HMS.Modules.Identity.Application",
                "HMS.Modules.Identity.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void SharedKernel_HasNoDependencyOnAspNetCoreEfCoreOrAnyModule()
    {
        var kernelAssembly = Assembly.Load("HMS.Shared.Kernel");

        var result = Types.InAssembly(kernelAssembly)
            .Should()
            .NotHaveDependencyOnAny("Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore", "HMS.Modules", "Npgsql")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void SharedInfrastructure_HasNoDependencyOnAnyModule()
    {
        var infrastructureAssembly = Assembly.Load("HMS.Shared.Infrastructure");

        var result = Types.InAssembly(infrastructureAssembly)
            .Should()
            .NotHaveDependencyOnAny("HMS.Modules")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
