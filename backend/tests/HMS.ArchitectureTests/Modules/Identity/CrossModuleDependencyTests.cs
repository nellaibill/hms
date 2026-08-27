using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.Identity;

/// <summary>
/// No module may depend on Identity's internals — only its Contracts, per
/// docs/Architecture.md §3. The other five modules have no code yet, so this rule is
/// vacuously true today; it exists to catch a violation the moment any of them add code
/// that reaches into Identity.
/// </summary>
public class CrossModuleDependencyTests
{
    [Theory]
    [InlineData("HMS.Modules.Patients")]
    [InlineData("HMS.Modules.Appointments")]
    [InlineData("HMS.Modules.Staff")]
    [InlineData("HMS.Modules.Billing")]
    [InlineData("HMS.Modules.Notifications")]
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
