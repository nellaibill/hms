using FluentAssertions;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Masters.Application;

public class DiagnosticServiceServiceTests
{
    private static readonly Guid CategoryId = Guid.NewGuid();
    private static readonly Guid ProviderId = Guid.NewGuid();

    private readonly IDiagnosticServiceRepository _repository = Substitute.For<IDiagnosticServiceRepository>();
    private readonly IDiagnosticCategoryService _categoryService = Substitute.For<IDiagnosticCategoryService>();
    private readonly IDiagnosticProviderService _providerService = Substitute.For<IDiagnosticProviderService>();
    private readonly DiagnosticServiceService _sut;

    public DiagnosticServiceServiceTests()
    {
        _sut = new DiagnosticServiceService(_repository, _categoryService, _providerService);

        // Happy-path defaults: the category and provider both exist. Failure-path tests
        // override these per-test — mirrors PatientVisitServiceTests' identical setup.
        _categoryService.GetByIdAsync(CategoryId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticCategoryResponse>.Success(new DiagnosticCategoryResponse { Id = CategoryId }));
        _providerService.GetByIdAsync(ProviderId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticProviderResponse>.Success(new DiagnosticProviderResponse { Id = ProviderId }));
    }

    private static CreateDiagnosticServiceRequest NewCreateRequest(bool isOutsourced = false, Guid? providerId = null) => new()
    {
        Code = "CBC",
        Name = "Complete Blood Count",
        CategoryId = CategoryId,
        ServiceType = DiagnosticTestServiceType.Laboratory,
        IsOutsourced = isOutsourced,
        ProviderId = providerId,
        Price = 250m,
        IsActive = true,
    };

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesServiceAndReturnsSuccess()
    {
        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("CBC");
        result.Value.CategoryId.Should().Be(CategoryId);
        await _repository.Received(1).AddAsync(Arg.Any<DiagnosticService>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithOutsourcedAndValidProvider_Succeeds()
    {
        var result = await _sut.CreateAsync(NewCreateRequest(isOutsourced: true, providerId: ProviderId), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProviderId.Should().Be(ProviderId);
    }

    [Fact]
    public async Task CreateAsync_WhenCodeAlreadyExists_ReturnsDuplicateCode()
    {
        _repository.ExistsByCodeAsync("CBC", excludingId: null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(MastersErrorCodes.DuplicateCode);
    }

    [Fact]
    public async Task CreateAsync_WhenCategoryDoesNotExist_ReturnsInvalidCategory()
    {
        _categoryService.GetByIdAsync(CategoryId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticCategoryResponse>.Failure(MastersErrorCodes.NotFound, "not found"));

        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(MastersErrorCodes.InvalidCategory);
    }

    [Fact]
    public async Task CreateAsync_WhenOutsourcedWithoutProvider_ReturnsInvalidProvider()
    {
        var result = await _sut.CreateAsync(NewCreateRequest(isOutsourced: true, providerId: null), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(MastersErrorCodes.InvalidProvider);
    }

    [Fact]
    public async Task CreateAsync_WhenOutsourcedProviderDoesNotExist_ReturnsInvalidProvider()
    {
        _providerService.GetByIdAsync(ProviderId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticProviderResponse>.Failure(MastersErrorCodes.NotFound, "not found"));

        var result = await _sut.CreateAsync(NewCreateRequest(isOutsourced: true, providerId: ProviderId), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(MastersErrorCodes.InvalidProvider);
    }

    [Fact]
    public async Task UpdateAsync_WhenServiceDoesNotExist_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DiagnosticService?)null);

        var request = new UpdateDiagnosticServiceRequest { Code = "CBC", Name = "Complete Blood Count", CategoryId = CategoryId, ServiceType = DiagnosticTestServiceType.Laboratory, Price = 250m, IsActive = true };
        var result = await _sut.UpdateAsync(Guid.NewGuid(), request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(MastersErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_WhenServiceDoesNotExist_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DiagnosticService?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(MastersErrorCodes.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_WhenServiceExists_SoftDeletesAndReturnsSuccess()
    {
        var diagnosticService = DiagnosticService.Create("CBC", "Complete Blood Count", CategoryId, DiagnosticTestServiceType.Laboratory, false, null, 250m, true, null);
        _repository.GetByIdAsync(diagnosticService.Id, Arg.Any<CancellationToken>()).Returns(diagnosticService);

        var result = await _sut.DeleteAsync(diagnosticService.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        diagnosticService.IsDeleted.Should().BeTrue();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
