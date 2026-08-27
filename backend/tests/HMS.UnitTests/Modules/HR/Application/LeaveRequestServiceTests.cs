using FluentAssertions;
using HMS.Modules.HR.Application;
using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.HR.Application;

public class LeaveRequestServiceTests
{
    private readonly ILeaveRequestRepository _repository = Substitute.For<ILeaveRequestRepository>();
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly ILeaveTypeRepository _leaveTypeRepository = Substitute.For<ILeaveTypeRepository>();
    private readonly LeaveRequestService _sut;
    private readonly Employee _employee;
    private readonly LeaveType _leaveType;

    public LeaveRequestServiceTests()
    {
        _sut = new LeaveRequestService(_repository, _employeeRepository, _leaveTypeRepository);

        _employee = Employee.Create(
            "EMP-1", "Ada", "Lovelace", Gender.Female, new DateOnly(1990, 1, 1), "p", "e@example.com", "addr", "c", "cp",
            Guid.NewGuid(), Guid.NewGuid(), EmployeeType.Permanent, new DateOnly(2024, 1, 1), EmploymentStatus.Active,
            null, null, null, true, null);
        _leaveType = LeaveType.Create("CL", "Casual Leave", 12, true, true, null);

        _employeeRepository.GetByIdAsync(_employee.Id, Arg.Any<CancellationToken>()).Returns(_employee);
        _leaveTypeRepository.GetByIdAsync(_leaveType.Id, Arg.Any<CancellationToken>()).Returns(_leaveType);
    }

    private CreateLeaveRequestRequest NewCreateRequest(DateOnly? start = null, DateOnly? end = null) => new()
    {
        EmployeeId = _employee.Id,
        LeaveTypeId = _leaveType.Id,
        StartDate = start ?? new DateOnly(2026, 9, 1),
        EndDate = end ?? new DateOnly(2026, 9, 3),
        Reason = "Family event",
    };

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesAndReturnsSuccess()
    {
        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalDays.Should().Be(3);
        result.Value.EmployeeName.Should().Be("Ada Lovelace");
        result.Value.LeaveTypeName.Should().Be("Casual Leave");
        await _repository.Received(1).AddAsync(Arg.Any<LeaveRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenEmployeeDoesNotExist_ReturnsInvalidEmployeeFailure()
    {
        _employeeRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.InvalidEmployee);
    }

    [Fact]
    public async Task CreateAsync_WhenLeaveTypeDoesNotExist_ReturnsInvalidLeaveTypeFailure()
    {
        _leaveTypeRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((LeaveType?)null);

        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.InvalidLeaveType);
    }

    [Fact]
    public async Task CreateAsync_WhenEndDateBeforeStartDate_ReturnsInvalidDateRangeFailure()
    {
        var result = await _sut.CreateAsync(NewCreateRequest(start: new DateOnly(2026, 9, 5), end: new DateOnly(2026, 9, 1)), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.InvalidDateRange);
        await _repository.DidNotReceive().AddAsync(Arg.Any<LeaveRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveAsync_WhenPending_ApprovesAndReturnsSuccess()
    {
        var leaveRequest = LeaveRequest.Create(_employee.Id, _leaveType.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1), "Reason", null);
        _repository.GetByIdAsync(leaveRequest.Id, Arg.Any<CancellationToken>()).Returns(leaveRequest);
        var approverId = Guid.NewGuid();

        var result = await _sut.ApproveAsync(leaveRequest.Id, new ApproveLeaveRequestRequest { Notes = "OK" }, approverId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(LeaveRequestStatus.Approved);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveAsync_WhenNotPending_ReturnsInvalidStatusTransitionFailure()
    {
        var leaveRequest = LeaveRequest.Create(_employee.Id, _leaveType.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1), "Reason", null);
        leaveRequest.Cancel(null);
        _repository.GetByIdAsync(leaveRequest.Id, Arg.Any<CancellationToken>()).Returns(leaveRequest);

        var result = await _sut.ApproveAsync(leaveRequest.Id, new ApproveLeaveRequestRequest(), actorUserId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.InvalidStatusTransition);
    }

    [Fact]
    public async Task RejectAsync_WhenPending_RejectsAndReturnsSuccess()
    {
        var leaveRequest = LeaveRequest.Create(_employee.Id, _leaveType.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1), "Reason", null);
        _repository.GetByIdAsync(leaveRequest.Id, Arg.Any<CancellationToken>()).Returns(leaveRequest);

        var result = await _sut.RejectAsync(leaveRequest.Id, new RejectLeaveRequestRequest { Reason = "Understaffed" }, actorUserId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(LeaveRequestStatus.Rejected);
        result.Value.DecisionNotes.Should().Be("Understaffed");
    }

    [Fact]
    public async Task RejectAsync_WhenNotPending_ReturnsInvalidStatusTransitionFailure()
    {
        var leaveRequest = LeaveRequest.Create(_employee.Id, _leaveType.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1), "Reason", null);
        leaveRequest.Approve(null, null);
        _repository.GetByIdAsync(leaveRequest.Id, Arg.Any<CancellationToken>()).Returns(leaveRequest);

        var result = await _sut.RejectAsync(leaveRequest.Id, new RejectLeaveRequestRequest { Reason = "x" }, actorUserId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.InvalidStatusTransition);
    }

    [Fact]
    public async Task CancelAsync_WhenPending_CancelsAndReturnsSuccess()
    {
        var leaveRequest = LeaveRequest.Create(_employee.Id, _leaveType.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1), "Reason", null);
        _repository.GetByIdAsync(leaveRequest.Id, Arg.Any<CancellationToken>()).Returns(leaveRequest);

        var result = await _sut.CancelAsync(leaveRequest.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(LeaveRequestStatus.Cancelled);
    }

    [Fact]
    public async Task CancelAsync_WhenNotPending_ReturnsInvalidStatusTransitionFailure()
    {
        var leaveRequest = LeaveRequest.Create(_employee.Id, _leaveType.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1), "Reason", null);
        leaveRequest.Reject(null, "no");
        _repository.GetByIdAsync(leaveRequest.Id, Arg.Any<CancellationToken>()).Returns(leaveRequest);

        var result = await _sut.CancelAsync(leaveRequest.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.InvalidStatusTransition);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((LeaveRequest?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPagedResult()
    {
        var leaveRequest = LeaveRequest.Create(_employee.Id, _leaveType.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1), "Reason", null);
        _repository.GetPagedAsync(Arg.Any<LeaveRequestListQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<LeaveRequestWithDetails> { new(leaveRequest, _employee.EmployeeCode, "Ada Lovelace", _leaveType.Name) }, 1));

        var result = await _sut.GetPagedAsync(new LeaveRequestListQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle(l => l.Id == leaveRequest.Id && l.LeaveTypeName == "Casual Leave");
        result.TotalCount.Should().Be(1);
    }
}
