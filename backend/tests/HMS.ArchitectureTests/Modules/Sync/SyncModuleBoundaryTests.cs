using System.Reflection;
using FluentAssertions;
using HMS.Modules.Sync;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.Sync;

/// <summary>
/// Module-boundary rules (docs/Architecture.md §3–4) for HMS.Modules.Sync (ADR-133):
/// internal layers stay internal except the narrow public seams; Sync uses other modules only
/// through their public seams; no other module depends on Sync; and Hangfire stays inside Sync.
/// </summary>
public class SyncModuleBoundaryTests
{
    private static readonly Assembly SyncAssembly = typeof(SyncModule).Assembly;

    // SyncDbContext / SyncRegistryDbContext: resolved by type from HMS.Api (migrations).
    // ISyncMonitorService / ISyncReceiverService / SyncMonitorErrorCodes: the controllers' public
    // constructor and response mapping need them (CS0051).
    private const string AllowedPublicTypeNamePattern =
        "^(SyncDbContext|SyncRegistryDbContext|ISyncMonitorService|ISyncReceiverService|SyncMonitorErrorCodes)$";

    [Theory]
    [InlineData("HMS.Modules.Sync.Domain")]
    [InlineData("HMS.Modules.Sync.Application")]
    [InlineData("HMS.Modules.Sync.Infrastructure")]
    public void InternalLayers_ShouldNotExposePublicTypes(string layerNamespace)
    {
        var result = Types.InAssembly(SyncAssembly)
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
        var result = Types.InAssembly(SyncAssembly)
            .That()
            .ResideInNamespace("HMS.Modules.Sync.Contracts")
            .Should()
            .BePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void Sync_ShouldUseOtherModulesOnlyThroughTheirPublicSeams()
    {
        var result = Types.InAssembly(SyncAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "HMS.Modules.Patients.Domain",
                "HMS.Modules.Patients.Infrastructure",
                "HMS.Modules.Platform.Domain",
                "HMS.Modules.Platform.Infrastructure",
                "HMS.Modules.ActivityLog.Domain",
                "HMS.Modules.ActivityLog.Infrastructure",
                "HMS.Modules.Identity",
                "HMS.Modules.Billing",
                "HMS.Modules.Masters")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Theory]
    [InlineData("HMS.Modules.Patients")]
    [InlineData("HMS.Modules.Identity")]
    [InlineData("HMS.Modules.Billing")]
    [InlineData("HMS.Modules.Masters")]
    [InlineData("HMS.Modules.ActivityLog")]
    public void OtherModules_ShouldNotDependOnSync(string otherModuleAssemblyName)
    {
        // Patients captures changes through HMS.Shared.Kernel.Sync.IOutboxWriter only.
        var result = Types.InAssembly(Assembly.Load(otherModuleAssemblyName))
            .Should()
            .NotHaveDependencyOn("HMS.Modules.Sync")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Theory]
    [InlineData("HMS.Modules.Patients")]
    [InlineData("HMS.Modules.Identity")]
    [InlineData("HMS.Modules.Billing")]
    [InlineData("HMS.Shared.Kernel")]
    [InlineData("HMS.Shared.Infrastructure")]
    public void Hangfire_ShouldStayInsideTheSyncModule(string otherAssemblyName)
    {
        var result = Types.InAssembly(Assembly.Load(otherAssemblyName))
            .Should()
            .NotHaveDependencyOn("Hangfire")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    private static string FailureMessage(TestResult result) =>
        result.FailingTypeNames is null
            ? "Rule failed."
            : "Rule failed for: " + string.Join(", ", result.FailingTypeNames);
}
