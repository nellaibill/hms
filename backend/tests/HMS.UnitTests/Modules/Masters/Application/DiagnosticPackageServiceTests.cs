using FluentAssertions;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Masters.Application;

public class DiagnosticPackageServiceTests
{
    private static readonly Guid ServiceId = Guid.NewGuid();

    private readonly IDiagnosticPackageRepository _repository = Substitute.For<IDiagnosticPackageRepository>();
    private readonly IDiagnosticServiceService _diagnosticServiceService = Substitute.For<IDiagnosticServiceService>();
    private readonly DiagnosticPackageService _sut;

    public DiagnosticPackageServiceTests()
    {
        _sut = new DiagnosticPackageService(_repository, _diagnosticServiceService);

        // Happy-path default: the referenced DiagnosticService exists. Failure-path tests
        // override this per-test.
        _diagnosticServiceService.GetByIdAsync(ServiceId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticServiceResponse>.Success(new DiagnosticServiceResponse { Id = ServiceId }));
    }

    private static CreateDiagnosticPackageRequest NewCreateRequest(params Guid[] serviceIds) => new()
    {
        Code = "MHC",
        Name = "Master Health Checkup",
        Description = "Comprehensive checkup bundle",
        TotalPrice = 1500m,
        IsActive = true,
        ServiceIds = serviceIds.Length > 0 ? serviceIds : [ServiceId],
    };

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesPackageAndReturnsSuccess()
    {
        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("MHC");
        result.Value.Items.Should().ContainSingle(i => i.ServiceId == ServiceId);
        await _repository.Received(1).AddAsync(Arg.Any<DiagnosticPackage>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenCodeAlreadyExists_ReturnsDuplicateCode()
    {
        _repository.ExistsByCodeAsync("MHC", excludingId: null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(MastersErrorCodes.DuplicateCode);
    }

    [Fact]
    public async Task CreateAsync_WhenAnItemServiceIdDoesNotExist_ReturnsInvalidPackageItemService()
    {
        var unknownServiceId = Guid.NewGuid();
        _diagnosticServiceService.GetByIdAsync(unknownServiceId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticServiceResponse>.Failure(MastersErrorCodes.NotFound, "not found"));

        var result = await _sut.CreateAsync(NewCreateRequest(unknownServiceId), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(MastersErrorCodes.InvalidPackageItemService);
    }

    [Fact]
    public async Task UpdateAsync_WhenPackageDoesNotExist_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DiagnosticPackage?)null);

        var request = new UpdateDiagnosticPackageRequest { Code = "MHC", Name = "Master Health Checkup", TotalPrice = 1500m, IsActive = true };
        var result = await _sut.UpdateAsync(Guid.NewGuid(), request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(MastersErrorCodes.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_DoesNotChangeTotalPriceBasedOnItems()
    {
        var package = DiagnosticPackage.Create("MHC", "Master Health Checkup", null, 1500m, true, [ServiceId], null);
        _repository.GetByIdAsync(package.Id, Arg.Any<CancellationToken>()).Returns(package);

        var request = new UpdateDiagnosticPackageRequest { Code = "MHC", Name = "Master Health Checkup", TotalPrice = 999m, IsActive = true };
        var result = await _sut.UpdateAsync(package.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalPrice.Should().Be(999m);
        result.Value.Items.Should().ContainSingle(i => i.ServiceId == ServiceId);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPackageDoesNotExist_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DiagnosticPackage?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(MastersErrorCodes.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_WhenPackageExists_SoftDeletesAndReturnsSuccess()
    {
        var package = DiagnosticPackage.Create("MHC", "Master Health Checkup", null, 1500m, true, [ServiceId], null);
        _repository.GetByIdAsync(package.Id, Arg.Any<CancellationToken>()).Returns(package);

        var result = await _sut.DeleteAsync(package.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        package.IsDeleted.Should().BeTrue();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddItemAsync_WithValidServiceId_AddsItemAndReturnsSuccess()
    {
        var package = DiagnosticPackage.Create("MHC", "Master Health Checkup", null, 1500m, true, [ServiceId], null);
        _repository.GetByIdAsync(package.Id, Arg.Any<CancellationToken>()).Returns(package);
        var newServiceId = Guid.NewGuid();
        _diagnosticServiceService.GetByIdAsync(newServiceId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticServiceResponse>.Success(new DiagnosticServiceResponse { Id = newServiceId }));

        var result = await _sut.AddItemAsync(package.Id, new AddDiagnosticPackageItemRequest { ServiceId = newServiceId }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddItemAsync_WhenServiceIdDoesNotExist_ReturnsInvalidPackageItemService()
    {
        var package = DiagnosticPackage.Create("MHC", "Master Health Checkup", null, 1500m, true, [ServiceId], null);
        _repository.GetByIdAsync(package.Id, Arg.Any<CancellationToken>()).Returns(package);
        var unknownServiceId = Guid.NewGuid();
        _diagnosticServiceService.GetByIdAsync(unknownServiceId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticServiceResponse>.Failure(MastersErrorCodes.NotFound, "not found"));

        var result = await _sut.AddItemAsync(package.Id, new AddDiagnosticPackageItemRequest { ServiceId = unknownServiceId }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(MastersErrorCodes.InvalidPackageItemService);
    }

    [Fact]
    public async Task RemoveItemAsync_WithExistingItem_RemovesItAndReturnsSuccess()
    {
        var package = DiagnosticPackage.Create("MHC", "Master Health Checkup", null, 1500m, true, [ServiceId], null);
        var itemId = package.Items.Single().Id;
        _repository.GetByIdAsync(package.Id, Arg.Any<CancellationToken>()).Returns(package);

        var result = await _sut.RemoveItemAsync(package.Id, itemId, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveItemAsync_WhenItemDoesNotExist_ReturnsNotFound()
    {
        var package = DiagnosticPackage.Create("MHC", "Master Health Checkup", null, 1500m, true, [ServiceId], null);
        _repository.GetByIdAsync(package.Id, Arg.Any<CancellationToken>()).Returns(package);

        var result = await _sut.RemoveItemAsync(package.Id, Guid.NewGuid(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(MastersErrorCodes.NotFound);
    }
}
