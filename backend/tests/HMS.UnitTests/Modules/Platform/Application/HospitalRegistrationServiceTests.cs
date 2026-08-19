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
    private readonly IHospitalRegistrationIdempotencyStore _idempotencyStore = Substitute.For<IHospitalRegistrationIdempotencyStore>();
    private readonly ILogger<HospitalRegistrationService> _logger = Substitute.For<ILogger<HospitalRegistrationService>>();
    private readonly HospitalRegistrationService _sut;
    private readonly Guid _platformAdminId = Guid.NewGuid();
    private const string IdempotencyKey = "test-idempotency-key";

    public HospitalRegistrationServiceTests()
    {
        _sut = new HospitalRegistrationService(_tenantRepository, _tenantProvisioner, _idempotencyStore, _logger);

        _tenantProvisioner.ProvisionAsync(Arg.Any<TenantProvisionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<TenantProvisionResult>.Success(new TenantProvisionResult("hms_tenant_apollo")));

        // Default: a fresh Idempotency-Key every test doesn't otherwise stub — RegisterAsync
        // should proceed exactly as it did before idempotency protection existed.
        _idempotencyStore.ReserveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservation(IdempotencyReservationOutcome.Reserved, RecordId: Guid.NewGuid()));
    }

    private Task<Result<CreateHospitalResponse>> Register(CreateHospitalRequest request, Guid? actorId) =>
        _sut.RegisterAsync(request, actorId, IdempotencyKey, CancellationToken.None);

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
        var result = await Register(ValidRequest(), _platformAdminId);

        result.IsSuccess.Should().BeTrue();
        await _tenantRepository.Received(1).AddAsync(
            Arg.Is<Tenant>(t => t.CreatedBy == _platformAdminId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_WithNoAuthenticatedActor_StoresNullCreatedBy()
    {
        var result = await Register(ValidRequest(), actorId: null);

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

        var result = await Register(ValidRequest(), _platformAdminId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PlatformErrorCodes.DuplicateHospitalCode);
        await _tenantProvisioner.DidNotReceive().ProvisionAsync(Arg.Any<TenantProvisionRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_WhenSuperAdminEmailAlreadyExists_ReturnsDuplicateFailureAndNeverProvisions()
    {
        _tenantRepository.GetByEmailAsync("admin@apollo.example", Arg.Any<CancellationToken>())
            .Returns(Tenant.Create("Existing", "existing", "9876543210", "existing@example.com", "1 MG Road", "Chennai", "Tamil Nadu", "600001", "hms_tenant_existing", createdBy: null));

        var result = await Register(ValidRequest(), _platformAdminId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PlatformErrorCodes.DuplicateAdminEmail);
        await _tenantProvisioner.DidNotReceive().ProvisionAsync(Arg.Any<TenantProvisionRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_WhenProvisioningFails_ReturnsFailureAndNeverWritesATenantRow()
    {
        _tenantProvisioner.ProvisionAsync(Arg.Any<TenantProvisionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<TenantProvisionResult>.Failure(TenantProvisioningErrorCodes.Failed, "database creation failed"));

        var result = await Register(ValidRequest(), _platformAdminId);

        result.IsSuccess.Should().BeFalse();
        await _tenantRepository.DidNotReceive().AddAsync(Arg.Any<Tenant>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_AfterSuccess_CompletesTheIdempotencyReservationWithTheSuccessResult()
    {
        var recordId = Guid.NewGuid();
        _idempotencyStore.ReserveAsync(IdempotencyKey, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservation(IdempotencyReservationOutcome.Reserved, RecordId: recordId));

        var result = await Register(ValidRequest(), _platformAdminId);

        result.IsSuccess.Should().BeTrue();
        await _idempotencyStore.Received(1).CompleteAsync(
            recordId,
            Arg.Is<Result<CreateHospitalResponse>>(r => r.IsSuccess),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_AfterProvisioningFailure_StillCompletesTheIdempotencyReservationWithTheFailureResult()
    {
        // If a failed attempt never completed its reservation, the Idempotency-Key would
        // stay "in progress" forever — the caller could never retry with the same key even
        // though nothing actually succeeded on the first attempt.
        var recordId = Guid.NewGuid();
        _idempotencyStore.ReserveAsync(IdempotencyKey, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservation(IdempotencyReservationOutcome.Reserved, RecordId: recordId));
        _tenantProvisioner.ProvisionAsync(Arg.Any<TenantProvisionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<TenantProvisionResult>.Failure(TenantProvisioningErrorCodes.Failed, "database creation failed"));

        var result = await Register(ValidRequest(), _platformAdminId);

        result.IsSuccess.Should().BeFalse();
        await _idempotencyStore.Received(1).CompleteAsync(
            recordId,
            Arg.Is<Result<CreateHospitalResponse>>(r => !r.IsSuccess),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_WhenAPriorRequestWithTheSameKeyAlreadyCompleted_ReplaysItWithoutProvisioningAgain()
    {
        var cachedResponse = new CreateHospitalResponse { Id = Guid.NewGuid(), HospitalCode = "apollo" };
        _idempotencyStore.ReserveAsync(IdempotencyKey, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservation(
                IdempotencyReservationOutcome.ReplayCompleted,
                ReplayedResult: Result<CreateHospitalResponse>.Success(cachedResponse)));

        var result = await Register(ValidRequest(), _platformAdminId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(cachedResponse);
        await _tenantProvisioner.DidNotReceive().ProvisionAsync(Arg.Any<TenantProvisionRequest>(), Arg.Any<CancellationToken>());
        await _tenantRepository.DidNotReceive().AddAsync(Arg.Any<Tenant>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_WhenAPriorRequestWithTheSameKeyIsStillInFlight_ReturnsInProgressFailureWithoutProvisioning()
    {
        _idempotencyStore.ReserveAsync(IdempotencyKey, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservation(IdempotencyReservationOutcome.ReplayInProgress));

        var result = await Register(ValidRequest(), _platformAdminId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PlatformErrorCodes.IdempotencyKeyInProgress);
        await _tenantProvisioner.DidNotReceive().ProvisionAsync(Arg.Any<TenantProvisionRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_WhenTheKeyWasReusedForADifferentRequest_ReturnsReusedFailureWithoutProvisioning()
    {
        _idempotencyStore.ReserveAsync(IdempotencyKey, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservation(IdempotencyReservationOutcome.KeyReusedForDifferentRequest));

        var result = await Register(ValidRequest(), _platformAdminId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PlatformErrorCodes.IdempotencyKeyReused);
        await _tenantProvisioner.DidNotReceive().ProvisionAsync(Arg.Any<TenantProvisionRequest>(), Arg.Any<CancellationToken>());
    }
}
