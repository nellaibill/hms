using FluentAssertions;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Masters.Application;

public class DiagnosticProviderServiceTests
{
    private readonly IDiagnosticProviderRepository _repository = Substitute.For<IDiagnosticProviderRepository>();
    private readonly DiagnosticProviderService _sut;

    public DiagnosticProviderServiceTests()
    {
        _sut = new DiagnosticProviderService(_repository);
    }

    private static CreateDiagnosticProviderRequest NewCreateRequest() => new()
    {
        Code = "QLAB",
        Name = "Q-LAB",
        ContactDetails = "contact@qlab.example",
        IsActive = true,
    };

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesProviderAndReturnsSuccess()
    {
        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("QLAB");
        result.Value.Name.Should().Be("Q-LAB");
        await _repository.Received(1).AddAsync(Arg.Any<DiagnosticProvider>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenCodeAlreadyExists_ReturnsDuplicateCode()
    {
        _repository.ExistsByCodeAsync("QLAB", excludingId: null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(MastersErrorCodes.DuplicateCode);
    }

    [Fact]
    public async Task UpdateAsync_WhenProviderDoesNotExist_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DiagnosticProvider?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateDiagnosticProviderRequest { Code = "QLAB", Name = "Q-LAB", IsActive = true }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(MastersErrorCodes.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesProviderAndReturnsSuccess()
    {
        var provider = DiagnosticProvider.Create("QLAB", "Q-LAB", null, true, null);
        _repository.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);

        var result = await _sut.UpdateAsync(provider.Id, new UpdateDiagnosticProviderRequest { Code = "QLAB", Name = "Q-LAB (Updated)", IsActive = true }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Q-LAB (Updated)");
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WhenProviderDoesNotExist_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DiagnosticProvider?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(MastersErrorCodes.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_WhenProviderExists_SoftDeletesAndReturnsSuccess()
    {
        var provider = DiagnosticProvider.Create("QLAB", "Q-LAB", null, true, null);
        _repository.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);

        var result = await _sut.DeleteAsync(provider.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        provider.IsDeleted.Should().BeTrue();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
