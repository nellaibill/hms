using FluentAssertions;
using HMS.Modules.Platform.Application;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Contracts;
using HMS.Modules.Platform.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Platform.Application;

public class TenantFeatureServiceTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly ITenantFeatureRepository _featureRepository = Substitute.For<ITenantFeatureRepository>();
    private readonly ITenantDirectory _tenantDirectory = Substitute.For<ITenantDirectory>();
    private readonly ITenantMigrationService _migrationService = Substitute.For<ITenantMigrationService>();
    private readonly ILogger<TenantFeatureService> _logger = Substitute.For<ILogger<TenantFeatureService>>();
    private readonly TenantFeatureService _sut;
    private readonly Guid _platformAdminId = Guid.NewGuid();

    public TenantFeatureServiceTests()
    {
        _sut = new TenantFeatureService(_tenantRepository, _featureRepository, _tenantDirectory, _migrationService, _logger);
    }

    private static Tenant NewTenant() => Tenant.Create(
        "Apollo", "apollo", "9876543210", "admin@apollo.example", "1 MG Road", "Chennai", "Tamil Nadu", "600001", "hms_tenant_apollo", createdBy: null);

    private void SeedTenant(Tenant tenant)
    {
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        _tenantDirectory.FindByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(new TenantInfo(tenant.Id, tenant.HospitalCode, tenant.DatabaseName, "Host=localhost;Database=hms_tenant_apollo", true, tenant.EnabledModules, FeatureCatalog.All));
    }

    [Fact]
    public async Task GetAsync_WhenTenantHasNoFeatureRows_DefaultsToEveryFeatureEnabled()
    {
        var tenant = NewTenant();
        SeedTenant(tenant);
        _featureRepository.GetByTenantIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(new List<TenantFeature>());

        var result = await _sut.GetAsync(tenant.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EnabledFeatures.Should().BeEquivalentTo(FeatureCatalog.All);
    }

    [Fact]
    public async Task GetAsync_WhenTenantNotFound_ReturnsNotFoundFailure()
    {
        _tenantRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var result = await _sut.GetAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PlatformErrorCodes.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_AlwaysForcesMandatoryFeaturesOnRegardlessOfRequest()
    {
        var tenant = NewTenant();
        SeedTenant(tenant);
        _featureRepository.GetByTenantIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(new List<TenantFeature>());

        // Caller sends an empty set — mandatory features must still end up enabled.
        var result = await _sut.UpdateAsync(tenant.Id, new UpdateTenantFeaturesRequest { EnabledFeatures = [] }, _platformAdminId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EnabledFeatures.Should().Contain(FeatureCatalog.Mandatory);
    }

    [Fact]
    public async Task UpdateAsync_WhenEnablingANewFeature_MigratesBeforeSaving()
    {
        var tenant = NewTenant();
        SeedTenant(tenant);
        _featureRepository.GetByTenantIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(new List<TenantFeature>());

        var result = await _sut.UpdateAsync(
            tenant.Id, new UpdateTenantFeaturesRequest { EnabledFeatures = ["hr"] }, _platformAdminId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _migrationService.Received(1).MigrateAsync(
            "Host=localhost;Database=hms_tenant_apollo",
            Arg.Is<IReadOnlyCollection<string>>(keys => keys.Contains("hr")),
            Arg.Any<CancellationToken>());
        await _featureRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenMigrationFails_ReturnsFailureAndSavesNothing()
    {
        var tenant = NewTenant();
        SeedTenant(tenant);
        _featureRepository.GetByTenantIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(new List<TenantFeature>());
        _migrationService.MigrateAsync(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("connection refused")));

        var result = await _sut.UpdateAsync(
            tenant.Id, new UpdateTenantFeaturesRequest { EnabledFeatures = ["hr"] }, _platformAdminId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PlatformErrorCodes.MigrationFailed);
        await _featureRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _featureRepository.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<TenantFeature>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenDisablingAnAlreadyEnabledFeature_NeverTriggersMigration()
    {
        var tenant = NewTenant();
        SeedTenant(tenant);
        var existing = FeatureCatalog.All.Select(key => TenantFeature.Create(tenant.Id, key, isEnabled: true, createdBy: null)).ToList();
        _featureRepository.GetByTenantIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(existing);

        // Disable every optional feature — only mandatory ones stay on.
        var result = await _sut.UpdateAsync(tenant.Id, new UpdateTenantFeaturesRequest { EnabledFeatures = [] }, _platformAdminId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EnabledFeatures.Should().BeEquivalentTo(FeatureCatalog.Mandatory);
        await _migrationService.DidNotReceive().MigrateAsync(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
    }
}
