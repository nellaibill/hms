using FluentAssertions;
using HMS.Modules.HR.Application;
using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.HR.Application;

public class StaffAvailabilityServiceTests
{
    private static readonly DateOnly StartDate = new(2026, 8, 3);
    private static readonly DateOnly EndDate = new(2026, 8, 10);

    private readonly IStaffAvailabilityRepository _repository = Substitute.For<IStaffAvailabilityRepository>();
    private readonly StaffAvailabilityService _sut;

    public StaffAvailabilityServiceTests()
    {
        _sut = new StaffAvailabilityService(_repository);
    }

    [Fact]
    public async Task CreateAsync_CreatesAvailabilityAndReturnsSuccess()
    {
        var request = new CreateStaffAvailabilityRequest
        {
            StaffId = Guid.NewGuid(),
            StartDate = StartDate,
            EndDate = EndDate,
            AvailabilityStatus = AvailabilityStatus.Unavailable,
            Reason = "Conference",
        };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AvailabilityStatus.Should().Be(AvailabilityStatus.Unavailable);
        result.Value.Reason.Should().Be("Conference");
        await _repository.Received(1).AddAsync(Arg.Any<StaffAvailability>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_UpdatesAndReturnsSuccess()
    {
        var availability = StaffAvailability.Create(Guid.NewGuid(), StartDate, EndDate, AvailabilityStatus.Available, null, null);
        _repository.GetByIdAsync(availability.Id, Arg.Any<CancellationToken>()).Returns(availability);

        var request = new UpdateStaffAvailabilityRequest
        {
            StaffId = availability.StaffId,
            StartDate = StartDate,
            EndDate = EndDate,
            AvailabilityStatus = AvailabilityStatus.Unavailable,
            Reason = "Medical Leave",
        };

        var result = await _sut.UpdateAsync(availability.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AvailabilityStatus.Should().Be(AvailabilityStatus.Unavailable);
        result.Value.Reason.Should().Be("Medical Leave");
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((StaffAvailability?)null);

        var request = new UpdateStaffAvailabilityRequest
        {
            StaffId = Guid.NewGuid(),
            StartDate = StartDate,
            EndDate = EndDate,
            AvailabilityStatus = AvailabilityStatus.Available,
        };

        var result = await _sut.UpdateAsync(Guid.NewGuid(), request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsSuccess()
    {
        var availability = StaffAvailability.Create(Guid.NewGuid(), StartDate, EndDate, AvailabilityStatus.Available, null, null);
        _repository.GetByIdAsync(availability.Id, Arg.Any<CancellationToken>()).Returns(availability);

        var result = await _sut.GetByIdAsync(availability.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(availability.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((StaffAvailability?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPagedResult()
    {
        var availability = StaffAvailability.Create(Guid.NewGuid(), StartDate, EndDate, AvailabilityStatus.Available, null, null);
        _repository.GetPagedAsync(Arg.Any<StaffAvailabilityListQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<StaffAvailability> { availability }, 1));

        var result = await _sut.GetPagedAsync(new StaffAvailabilityListQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle(a => a.Id == availability.Id);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_SoftDeletesAndReturnsSuccess()
    {
        var availability = StaffAvailability.Create(Guid.NewGuid(), StartDate, EndDate, AvailabilityStatus.Available, null, null);
        _repository.GetByIdAsync(availability.Id, Arg.Any<CancellationToken>()).Returns(availability);

        var result = await _sut.DeleteAsync(availability.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        availability.IsDeleted.Should().BeTrue();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((StaffAvailability?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.NotFound);
    }
}
