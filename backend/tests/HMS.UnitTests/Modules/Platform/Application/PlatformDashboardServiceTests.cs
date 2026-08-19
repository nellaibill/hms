using FluentAssertions;
using HMS.Modules.Platform.Application;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Contracts;
using HMS.Modules.Platform.Domain;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Platform.Application;

public class PlatformDashboardServiceTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly ITenantDirectory _tenantDirectory = Substitute.For<ITenantDirectory>();
    private readonly ITenantMigrationService _migrationService = Substitute.For<ITenantMigrationService>();
    private readonly IProvisioningAlertStore _provisioningAlertStore = Substitute.For<IProvisioningAlertStore>();
    private readonly ILogger<PlatformDashboardService> _logger = Substitute.For<ILogger<PlatformDashboardService>>();
    private readonly PlatformDashboardService _sut;
    private readonly Guid _platformAdminId = Guid.NewGuid();

    public PlatformDashboardServiceTests()
    {
        _sut = new PlatformDashboardService(_tenantRepository, _tenantDirectory, _migrationService, _provisioningAlertStore, _logger);
    }

    private static Tenant NewTenant() => Tenant.Create(
        "Apollo", "apollo", "9876543210", "admin@apollo.example", "1 MG Road", "Chennai", "Tamil Nadu", "600001", "hms_tenant_apollo", createdBy: null);

    [Fact]
    public async Task UpdateStatusAsync_WithValidStatus_RecordsTheCallingPlatformAdminAsUpdatedBy()
    {
        var tenant = NewTenant();
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await _sut.UpdateStatusAsync(tenant.Id, "Inactive", _platformAdminId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tenant.UpdatedBy.Should().Be(_platformAdminId);
        tenant.Status.Should().Be(TenantStatus.Inactive);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithNoAuthenticatedActor_StoresNullUpdatedBy()
    {
        var tenant = NewTenant();
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        await _sut.UpdateStatusAsync(tenant.Id, "Inactive", actorId: null, CancellationToken.None);

        tenant.UpdatedBy.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_WithInvalidStatus_ReturnsFailureAndLeavesTenantUntouched()
    {
        var tenant = NewTenant();
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await _sut.UpdateStatusAsync(tenant.Id, "NotAStatus", _platformAdminId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PlatformErrorCodes.InvalidStatus);
        tenant.UpdatedBy.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenTenantNotFound_ReturnsNotFoundFailure()
    {
        _tenantRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var result = await _sut.UpdateStatusAsync(Guid.NewGuid(), "Active", _platformAdminId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PlatformErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetStatsAsync_IncludesTheProvisioningAlertCount()
    {
        _tenantRepository.GetCountsAsync(Arg.Any<CancellationToken>()).Returns((5, 4, 1));
        _provisioningAlertStore.GetCountAsync(Arg.Any<CancellationToken>()).Returns(2);

        var stats = await _sut.GetStatsAsync(CancellationToken.None);

        stats.Total.Should().Be(5);
        stats.Active.Should().Be(4);
        stats.Inactive.Should().Be(1);
        stats.ProvisioningAlertCount.Should().Be(2);
    }
}
