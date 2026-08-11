using FluentAssertions;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Shared.Kernel;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.IPD.Application;

public class WardServiceTests
{
    private readonly IWardRepository _repository = Substitute.For<IWardRepository>();
    private readonly IDepartmentService _departmentService = Substitute.For<IDepartmentService>();
    private readonly WardService _sut;

    public WardServiceTests()
    {
        _sut = new WardService(_repository, _departmentService);

        _departmentService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<DepartmentResponse>.Success(new DepartmentResponse()));
    }

    private static CreateWardRequest NewCreateRequest() => new()
    {
        Code = "genmed",
        Name = "General Medicine Ward A",
        DepartmentId = Guid.NewGuid(),
        WardType = WardType.General,
        IsActive = true,
    };

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesAndReturnsSuccess()
    {
        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("GENMED");
        await _repository.Received(1).AddAsync(Arg.Any<Ward>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenCodeAlreadyExists_ReturnsDuplicateCodeFailure()
    {
        _repository.ExistsByCodeAsync("GENMED", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.DuplicateCode);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Ward>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenDepartmentDoesNotExist_ReturnsInvalidDepartmentFailure()
    {
        _departmentService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<DepartmentResponse>.Failure("MASTERS.NOT_FOUND", "not found"));

        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.InvalidDepartment);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Ward>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Ward?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateWardRequest { Name = "X", DepartmentId = Guid.NewGuid(), WardType = WardType.General, IsActive = true }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_SoftDeletesAndReturnsSuccess()
    {
        var ward = Ward.Create("genmed", "General Medicine Ward A", Guid.NewGuid(), WardType.General, true, null);
        _repository.GetByIdAsync(ward.Id, Arg.Any<CancellationToken>()).Returns(ward);

        var result = await _sut.DeleteAsync(ward.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        ward.IsDeleted.Should().BeTrue();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
