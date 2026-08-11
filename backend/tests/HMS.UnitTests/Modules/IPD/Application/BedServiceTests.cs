using FluentAssertions;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.IPD.Application;

public class BedServiceTests
{
    private readonly IBedRepository _repository = Substitute.For<IBedRepository>();
    private readonly IWardRepository _wardRepository = Substitute.For<IWardRepository>();
    private readonly BedService _sut;
    private readonly Guid _wardId = Guid.NewGuid();

    public BedServiceTests()
    {
        _sut = new BedService(_repository, _wardRepository);

        var ward = Ward.Create("genmed", "General Medicine Ward A", Guid.NewGuid(), WardType.General, true, null);
        _wardRepository.GetByIdAsync(_wardId, Arg.Any<CancellationToken>()).Returns(ward);
    }

    private CreateBedRequest NewCreateRequest() => new()
    {
        WardId = _wardId,
        BedNumber = "b-101",
        BedType = "Standard",
        Status = BedStatus.Available,
        IsActive = true,
    };

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesAndReturnsSuccess()
    {
        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BedNumber.Should().Be("B-101");
        await _repository.Received(1).AddAsync(Arg.Any<Bed>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenWardDoesNotExist_ReturnsInvalidWardFailure()
    {
        _wardRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Ward?)null);

        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.InvalidWard);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Bed>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenBedNumberAlreadyExistsInWard_ReturnsDuplicateBedNumberFailure()
    {
        _repository.ExistsByBedNumberAsync(_wardId, "B-101", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.DuplicateBedNumber);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Bed>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenBedIsOccupied_ReturnsBedOccupiedFailure()
    {
        var bed = Bed.Create(_wardId, "b-101", "Standard", BedStatus.Occupied, true, null);
        _repository.GetByIdAsync(bed.Id, Arg.Any<CancellationToken>()).Returns(bed);

        var result = await _sut.DeleteAsync(bed.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.BedOccupied);
        bed.IsDeleted.Should().BeFalse();
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenBedIsAvailable_SoftDeletesAndReturnsSuccess()
    {
        var bed = Bed.Create(_wardId, "b-101", "Standard", BedStatus.Available, true, null);
        _repository.GetByIdAsync(bed.Id, Arg.Any<CancellationToken>()).Returns(bed);

        var result = await _sut.DeleteAsync(bed.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        bed.IsDeleted.Should().BeTrue();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
