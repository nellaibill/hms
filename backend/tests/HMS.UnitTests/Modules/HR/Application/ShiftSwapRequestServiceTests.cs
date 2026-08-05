using FluentAssertions;
using HMS.Modules.HR.Application;
using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using HMS.Modules.Identity.Application;
using HMS.Modules.Identity.Contracts;
using HMS.Shared.Kernel;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.HR.Application;

public class ShiftSwapRequestServiceTests
{
    private static readonly DateTime RequestedDate = new(2026, 8, 3, 9, 0, 0, DateTimeKind.Utc);

    private readonly IShiftSwapRequestRepository _repository = Substitute.For<IShiftSwapRequestRepository>();
    private readonly IShiftAssignmentRepository _shiftAssignmentRepository = Substitute.For<IShiftAssignmentRepository>();
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly ShiftSwapRequestService _sut;
    private readonly Guid _currentAssignmentId = Guid.NewGuid();
    private readonly Guid _requestedAssignmentId = Guid.NewGuid();

    public ShiftSwapRequestServiceTests()
    {
        _sut = new ShiftSwapRequestService(_repository, _shiftAssignmentRepository, _userService);

        // Happy-path default: both referenced shift assignments exist, and every staff id
        // resolves to a real user. Tests for the failure paths override these per-test.
        var shiftAssignment = ShiftAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 8, 3), AssignmentStatus.Scheduled, null, null);
        _shiftAssignmentRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(shiftAssignment);
        _userService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Result<UserResponse>.Success(new UserResponse()));
    }

    private CreateSwapRequest NewCreateRequest() => new()
    {
        RequestedByStaffId = Guid.NewGuid(),
        RequestedToStaffId = Guid.NewGuid(),
        CurrentShiftAssignmentId = _currentAssignmentId,
        RequestedShiftAssignmentId = _requestedAssignmentId,
        Status = SwapRequestStatus.Pending,
        RequestedDate = RequestedDate,
    };

    [Fact]
    public async Task CreateAsync_WhenBothShiftAssignmentsExist_CreatesRequestAndReturnsSuccess()
    {
        var request = NewCreateRequest();

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CurrentShiftAssignmentId.Should().Be(_currentAssignmentId);
        result.Value.RequestedShiftAssignmentId.Should().Be(_requestedAssignmentId);
        await _repository.Received(1).AddAsync(Arg.Any<ShiftSwapRequest>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenCurrentShiftAssignmentDoesNotExist_ReturnsInvalidShiftAssignmentFailure()
    {
        _shiftAssignmentRepository.GetByIdAsync(_currentAssignmentId, Arg.Any<CancellationToken>()).Returns((ShiftAssignment?)null);
        var request = NewCreateRequest();

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.InvalidShiftAssignment);
        await _repository.DidNotReceive().AddAsync(Arg.Any<ShiftSwapRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenRequestedShiftAssignmentDoesNotExist_ReturnsInvalidShiftAssignmentFailure()
    {
        _shiftAssignmentRepository.GetByIdAsync(_requestedAssignmentId, Arg.Any<CancellationToken>()).Returns((ShiftAssignment?)null);
        var request = NewCreateRequest();

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.InvalidShiftAssignment);
        await _repository.DidNotReceive().AddAsync(Arg.Any<ShiftSwapRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenRequestedByStaffDoesNotExist_ReturnsInvalidStaffFailure()
    {
        var request = NewCreateRequest();
        _userService.GetByIdAsync(request.RequestedByStaffId, Arg.Any<CancellationToken>())
            .Returns(Result<UserResponse>.Failure("IDENTITY.USER_NOT_FOUND", "not found"));

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.InvalidStaff);
        await _repository.DidNotReceive().AddAsync(Arg.Any<ShiftSwapRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenFoundAndBothAssignmentsExist_UpdatesAndReturnsSuccess()
    {
        var swapRequest = ShiftSwapRequest.Create(Guid.NewGuid(), Guid.NewGuid(), _currentAssignmentId, _requestedAssignmentId, SwapRequestStatus.Pending, RequestedDate, null, null, null, null);
        _repository.GetByIdAsync(swapRequest.Id, Arg.Any<CancellationToken>()).Returns(swapRequest);

        var request = new UpdateSwapRequest
        {
            RequestedByStaffId = swapRequest.RequestedByStaffId,
            RequestedToStaffId = swapRequest.RequestedToStaffId,
            CurrentShiftAssignmentId = _currentAssignmentId,
            RequestedShiftAssignmentId = _requestedAssignmentId,
            Status = SwapRequestStatus.Approved,
            RequestedDate = RequestedDate,
            ApprovedBy = Guid.NewGuid(),
            ApprovedDate = DateTime.UtcNow,
        };

        var result = await _sut.UpdateAsync(swapRequest.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(SwapRequestStatus.Approved);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNotFoundFailure_AndNeverChecksShiftAssignments()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ShiftSwapRequest?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), NewCreateRequestAsUpdate(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_WhenNewCurrentShiftAssignmentDoesNotExist_ReturnsInvalidShiftAssignmentFailure()
    {
        var swapRequest = ShiftSwapRequest.Create(Guid.NewGuid(), Guid.NewGuid(), _currentAssignmentId, _requestedAssignmentId, SwapRequestStatus.Pending, RequestedDate, null, null, null, null);
        _repository.GetByIdAsync(swapRequest.Id, Arg.Any<CancellationToken>()).Returns(swapRequest);
        var newAssignmentId = Guid.NewGuid();
        _shiftAssignmentRepository.GetByIdAsync(newAssignmentId, Arg.Any<CancellationToken>()).Returns((ShiftAssignment?)null);

        var request = NewCreateRequestAsUpdate();
        request = request with { CurrentShiftAssignmentId = newAssignmentId };

        var result = await _sut.UpdateAsync(swapRequest.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.InvalidShiftAssignment);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private UpdateSwapRequest NewCreateRequestAsUpdate() => new()
    {
        RequestedByStaffId = Guid.NewGuid(),
        RequestedToStaffId = Guid.NewGuid(),
        CurrentShiftAssignmentId = _currentAssignmentId,
        RequestedShiftAssignmentId = _requestedAssignmentId,
        Status = SwapRequestStatus.Pending,
        RequestedDate = RequestedDate,
    };

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsSuccess()
    {
        var swapRequest = ShiftSwapRequest.Create(Guid.NewGuid(), Guid.NewGuid(), _currentAssignmentId, _requestedAssignmentId, SwapRequestStatus.Pending, RequestedDate, null, null, null, null);
        _repository.GetByIdAsync(swapRequest.Id, Arg.Any<CancellationToken>()).Returns(swapRequest);

        var result = await _sut.GetByIdAsync(swapRequest.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(swapRequest.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ShiftSwapRequest?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPagedResult()
    {
        var swapRequest = ShiftSwapRequest.Create(Guid.NewGuid(), Guid.NewGuid(), _currentAssignmentId, _requestedAssignmentId, SwapRequestStatus.Pending, RequestedDate, null, null, null, null);
        _repository.GetPagedAsync(Arg.Any<SwapRequestListQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<ShiftSwapRequest> { swapRequest }, 1));

        var result = await _sut.GetPagedAsync(new SwapRequestListQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle(s => s.Id == swapRequest.Id);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_SoftDeletesAndReturnsSuccess()
    {
        var swapRequest = ShiftSwapRequest.Create(Guid.NewGuid(), Guid.NewGuid(), _currentAssignmentId, _requestedAssignmentId, SwapRequestStatus.Pending, RequestedDate, null, null, null, null);
        _repository.GetByIdAsync(swapRequest.Id, Arg.Any<CancellationToken>()).Returns(swapRequest);

        var result = await _sut.DeleteAsync(swapRequest.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        swapRequest.IsDeleted.Should().BeTrue();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ShiftSwapRequest?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.NotFound);
    }
}
