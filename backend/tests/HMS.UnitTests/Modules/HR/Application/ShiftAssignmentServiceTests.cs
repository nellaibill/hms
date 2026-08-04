using FluentAssertions;
using HMS.Modules.HR.Application;
using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.HR.Application;

public class ShiftAssignmentServiceTests
{
    private static readonly DateOnly RosterDate = new(2026, 8, 4);

    private readonly IShiftAssignmentRepository _repository = Substitute.For<IShiftAssignmentRepository>();
    private readonly IShiftRepository _shiftRepository = Substitute.For<IShiftRepository>();
    private readonly ShiftAssignmentService _sut;
    private readonly Guid _shiftId = Guid.NewGuid();

    public ShiftAssignmentServiceTests()
    {
        _sut = new ShiftAssignmentService(_repository, _shiftRepository);

        // Happy-path default: a valid shift exists. Tests for the "shift not found" failure
        // path override this per-test.
        var shift = Shift.Create("morning", "Morning Shift", new TimeOnly(8, 0), new TimeOnly(16, 0), 0, 0, false, true, null);
        _shiftRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(shift);
    }

    private static CreateShiftAssignmentRequest NewCreateRequest(Guid shiftId) => new()
    {
        StaffId = Guid.NewGuid(),
        DepartmentId = Guid.NewGuid(),
        ShiftId = shiftId,
        RosterDate = RosterDate,
        Status = AssignmentStatus.Scheduled,
    };

    [Fact]
    public async Task CreateAsync_WithAnExistingShift_CreatesAssignmentAndReturnsSuccess()
    {
        var request = NewCreateRequest(_shiftId);

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ShiftId.Should().Be(_shiftId);
        result.Value.RosterDate.Should().Be(RosterDate);
        await _repository.Received(1).AddAsync(Arg.Any<ShiftAssignment>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenShiftDoesNotExist_ReturnsInvalidShiftFailureAndSavesNothing()
    {
        _shiftRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Shift?)null);
        var request = NewCreateRequest(Guid.NewGuid());

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.InvalidShift);
        await _repository.DidNotReceive().AddAsync(Arg.Any<ShiftAssignment>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_AllowsTheSameStaffAndDateAsAnExistingAssignment_NoOverlapCheck()
    {
        // Phase 2 explicitly defers this — the service never even queries for existing
        // assignments before creating a new one.
        var request = NewCreateRequest(_shiftId);

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.DidNotReceive().GetPagedAsync(Arg.Any<ShiftAssignmentListQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenFoundAndShiftExists_UpdatesAndReturnsSuccess()
    {
        var assignment = ShiftAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), _shiftId, RosterDate, AssignmentStatus.Scheduled, null, null);
        _repository.GetByIdAsync(assignment.Id, Arg.Any<CancellationToken>()).Returns(assignment);

        var newShiftId = Guid.NewGuid();
        var request = new UpdateShiftAssignmentRequest
        {
            StaffId = assignment.StaffId,
            DepartmentId = assignment.DepartmentId,
            ShiftId = newShiftId,
            RosterDate = new DateOnly(2026, 8, 5),
            Status = AssignmentStatus.Completed,
            Remarks = "Done",
        };

        var result = await _sut.UpdateAsync(assignment.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(AssignmentStatus.Completed);
        result.Value.ShiftId.Should().Be(newShiftId);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenAssignmentNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ShiftAssignment?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), NewUpdateRequest(_shiftId), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_WhenNewShiftIdDoesNotExist_ReturnsInvalidShiftFailure()
    {
        var assignment = ShiftAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), _shiftId, RosterDate, AssignmentStatus.Scheduled, null, null);
        _repository.GetByIdAsync(assignment.Id, Arg.Any<CancellationToken>()).Returns(assignment);
        _shiftRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Shift?)null);

        var result = await _sut.UpdateAsync(assignment.Id, NewUpdateRequest(Guid.NewGuid()), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.InvalidShift);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static UpdateShiftAssignmentRequest NewUpdateRequest(Guid shiftId) => new()
    {
        StaffId = Guid.NewGuid(),
        DepartmentId = Guid.NewGuid(),
        ShiftId = shiftId,
        RosterDate = RosterDate,
        Status = AssignmentStatus.Scheduled,
    };

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsSuccess()
    {
        var assignment = ShiftAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), _shiftId, RosterDate, AssignmentStatus.Scheduled, null, null);
        _repository.GetByIdAsync(assignment.Id, Arg.Any<CancellationToken>()).Returns(assignment);

        var result = await _sut.GetByIdAsync(assignment.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(assignment.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ShiftAssignment?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPagedResult()
    {
        var assignment = ShiftAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), _shiftId, RosterDate, AssignmentStatus.Scheduled, null, null);
        _repository.GetPagedAsync(Arg.Any<ShiftAssignmentListQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<ShiftAssignment> { assignment }, 1));

        var result = await _sut.GetPagedAsync(new ShiftAssignmentListQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle(a => a.Id == assignment.Id);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_SoftDeletesAndReturnsSuccess()
    {
        var assignment = ShiftAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), _shiftId, RosterDate, AssignmentStatus.Scheduled, null, null);
        _repository.GetByIdAsync(assignment.Id, Arg.Any<CancellationToken>()).Returns(assignment);

        var result = await _sut.DeleteAsync(assignment.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        assignment.IsDeleted.Should().BeTrue();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ShiftAssignment?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.NotFound);
    }
}
