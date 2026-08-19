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

    [Fact]
    public async Task GetDeletePreviewAsync_ReturnsTheTenantsCurrentInfo()
    {
        var tenant = NewTenant();
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await _sut.GetDeletePreviewAsync(tenant.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.HospitalCode.Should().Be(tenant.HospitalCode);
        result.Value.Status.Should().Be(tenant.Status.ToString());
    }

    [Fact]
    public async Task GetDeletePreviewAsync_WhenTenantNotFound_ReturnsNotFoundFailure()
    {
        _tenantRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var result = await _sut.GetDeletePreviewAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PlatformErrorCodes.NotFound);
    }

    [Fact]
    public async Task DeleteHospitalAsync_WithMatchingConfirmationCode_SoftDeletesTheTenant()
    {
        var tenant = NewTenant();
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await _sut.DeleteHospitalAsync(tenant.Id, "apollo", _platformAdminId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tenant.IsDeleted.Should().BeTrue();
        tenant.DeletedBy.Should().Be(_platformAdminId);
        await _tenantRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteHospitalAsync_ConfirmationIsCaseInsensitiveAndTrimmed()
    {
        var tenant = NewTenant();
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await _sut.DeleteHospitalAsync(tenant.Id, "  APOLLO  ", _platformAdminId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tenant.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteHospitalAsync_WithWrongConfirmationCode_LeavesTheTenantUntouched()
    {
        var tenant = NewTenant();
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await _sut.DeleteHospitalAsync(tenant.Id, "not-apollo", _platformAdminId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PlatformErrorCodes.ConfirmationMismatch);
        tenant.IsDeleted.Should().BeFalse();
        await _tenantRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteHospitalAsync_WhenTenantNotFound_ReturnsNotFoundFailure()
    {
        _tenantRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var result = await _sut.DeleteHospitalAsync(Guid.NewGuid(), "anything", _platformAdminId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PlatformErrorCodes.NotFound);
    }

    [Fact]
    public async Task RestoreHospitalAsync_WithASoftDeletedTenant_ClearsTheDeletedFlag()
    {
        var tenant = NewTenant();
        tenant.SoftDelete(Guid.NewGuid());
        _tenantRepository.GetByIdIncludingDeletedAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await _sut.RestoreHospitalAsync(tenant.Id, _platformAdminId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tenant.IsDeleted.Should().BeFalse();
        tenant.DeletedAt.Should().BeNull();
        tenant.DeletedBy.Should().BeNull();
        tenant.UpdatedBy.Should().Be(_platformAdminId);
    }

    [Fact]
    public async Task RestoreHospitalAsync_WhenTenantIsNotDeleted_ReturnsNotDeletedFailure()
    {
        var tenant = NewTenant();
        _tenantRepository.GetByIdIncludingDeletedAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await _sut.RestoreHospitalAsync(tenant.Id, _platformAdminId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PlatformErrorCodes.NotDeleted);
    }

    [Fact]
    public async Task RestoreHospitalAsync_WhenTenantDoesNotExistAtAll_ReturnsNotDeletedFailure()
    {
        _tenantRepository.GetByIdIncludingDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var result = await _sut.RestoreHospitalAsync(Guid.NewGuid(), _platformAdminId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PlatformErrorCodes.NotDeleted);
    }

    [Fact]
    public async Task GetDeletedHospitalsAsync_ReturnsThePagedDeletedList()
    {
        var tenant = NewTenant();
        tenant.SoftDelete(_platformAdminId);
        _tenantRepository.GetDeletedPagedAsync(Arg.Any<TenantListQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<Tenant> { tenant }, 1));

        var paged = await _sut.GetDeletedHospitalsAsync(new TenantListQuery(), CancellationToken.None);

        paged.TotalCount.Should().Be(1);
        paged.Items.Single().HospitalCode.Should().Be(tenant.HospitalCode);
        paged.Items.Single().DeletedAt.Should().Be(tenant.DeletedAt!.Value);
    }
}
