using FluentAssertions;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Masters.Application;

public class DiagnosticCategoryServiceTests
{
    private readonly IDiagnosticCategoryRepository _repository = Substitute.For<IDiagnosticCategoryRepository>();
    private readonly DiagnosticCategoryService _sut;

    public DiagnosticCategoryServiceTests()
    {
        _sut = new DiagnosticCategoryService(_repository);
    }

    private static CreateDiagnosticCategoryRequest NewCreateRequest() => new()
    {
        Code = "HEMA",
        Name = "Hematology",
        Description = "Blood-related tests",
        IsActive = true,
    };

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesCategoryAndReturnsSuccess()
    {
        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("HEMA");
        result.Value.Name.Should().Be("Hematology");
        await _repository.Received(1).AddAsync(Arg.Any<DiagnosticCategory>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenCodeAlreadyExists_ReturnsDuplicateCode()
    {
        _repository.ExistsByCodeAsync("HEMA", excludingId: null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(MastersErrorCodes.DuplicateCode);
    }

    [Fact]
    public async Task UpdateAsync_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DiagnosticCategory?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateDiagnosticCategoryRequest { Code = "HEMA", Name = "Hematology", IsActive = true }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(MastersErrorCodes.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesCategoryAndReturnsSuccess()
    {
        var category = DiagnosticCategory.Create("HEMA", "Hematology", null, true, null);
        _repository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _sut.UpdateAsync(category.Id, new UpdateDiagnosticCategoryRequest { Code = "HEMA", Name = "Hematology (Updated)", IsActive = true }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Hematology (Updated)");
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DiagnosticCategory?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(MastersErrorCodes.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_WhenCategoryExists_SoftDeletesAndReturnsSuccess()
    {
        var category = DiagnosticCategory.Create("HEMA", "Hematology", null, true, null);
        _repository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _sut.DeleteAsync(category.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        category.IsDeleted.Should().BeTrue();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
