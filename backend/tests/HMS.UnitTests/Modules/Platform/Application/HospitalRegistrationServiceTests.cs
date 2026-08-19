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

public class HospitalRegistrationServiceTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly ITenantProvisioner _tenantProvisioner = Substitute.For<ITenantProvisioner>();
    private readonly ILogger<HospitalRegistrationService> _logger = Substitute.For<ILogger<HospitalRegistrationService>>();
    private readonly HospitalRegistrationService _sut;
    private readonly Guid _platformAdminId = Guid.NewGuid();

    public HospitalRegistrationServiceTests()
    {
        _sut = new HospitalRegistrationService(_tenantRepository, _tenantProvisioner, _logger);

        _tenantProvisioner.ProvisionAsync(Arg.Any<TenantProvisionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<TenantProvisionResult>.Success(new TenantProvisionResult("hms_tenant_apollo")));
    }

    private static CreateHospitalRequest ValidRequest() => new()
    {
        HospitalName = "Apollo",
        HospitalCode = "apollo",
        MobileNumber = "9876543210",
        Address = "1 MG Road",
        City = "Chennai",
        State = "Tamil Nadu",
        Pincode = "600001",
        SuperAdminUsername = "apollo.admin",
        SuperAdminFirstName = "Admin",
        SuperAdminLastName = "User",
        SuperAdminEmail = "admin@apollo.example",
        SuperAdminPhoneNumber = "9876543211",
        SuperAdminPassword = "Sup3rAdmin!",
    };

    [Fact]
    public async Task RegisterAsync_WithValidRequest_RecordsTheCallingPlatformAdminAsCreatedBy()
    {
        var result = await _sut.RegisterAsync(ValidRequest(), _platformAdminId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _tenantRepository.Received(1).AddAsync(
            Arg.Is<Tenant>(t => t.CreatedBy == _platformAdminId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_WithNoAuthenticatedActor_StoresNullCreatedBy()
    {
        var result = await _sut.RegisterAsync(ValidRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _tenantRepository.Received(1).AddAsync(
            Arg.Is<Tenant>(t => t.CreatedBy == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_WhenHospitalCodeAlreadyExists_ReturnsDuplicateFailureAndNeverProvisions()
    {
        _tenantRepository.GetByHospitalCodeAsync("apollo", Arg.Any<CancellationToken>())
            .Returns(Tenant.Create("Apollo", "apollo", "9876543210", "existing@apollo.example", "1 MG Road", "Chennai", "Tamil Nadu", "600001", "hms_tenant_apollo", createdBy: null));

        var result = await _sut.RegisterAsync(ValidRequest(), _platformAdminId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PlatformErrorCodes.DuplicateHospitalCode);
        await _tenantProvisioner.DidNotReceive().ProvisionAsync(Arg.Any<TenantProvisionRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_WhenProvisioningFails_ReturnsFailureAndNeverWritesATenantRow()
    {
        _tenantProvisioner.ProvisionAsync(Arg.Any<TenantProvisionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<TenantProvisionResult>.Failure(TenantProvisioningErrorCodes.Failed, "database creation failed"));

        var result = await _sut.RegisterAsync(ValidRequest(), _platformAdminId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _tenantRepository.DidNotReceive().AddAsync(Arg.Any<Tenant>(), Arg.Any<CancellationToken>());
    }
}
