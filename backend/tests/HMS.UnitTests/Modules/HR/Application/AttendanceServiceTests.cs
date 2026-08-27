using FluentAssertions;
using HMS.Modules.HR.Application;
using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.HR.Application;

public class AttendanceServiceTests
{
    private static readonly DateOnly AttendanceDate = new(2026, 8, 27);

    private readonly IAttendanceRepository _repository = Substitute.For<IAttendanceRepository>();
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly AttendanceService _sut;
    private readonly Employee _employee;

    public AttendanceServiceTests()
    {
        _sut = new AttendanceService(_repository, _employeeRepository);

        _employee = Employee.Create(
            "EMP-1", "Ada", "Lovelace", Gender.Female, new DateOnly(1990, 1, 1), "p", "e@example.com", "addr", "c", "cp",
            Guid.NewGuid(), Guid.NewGuid(), EmployeeType.Permanent, new DateOnly(2024, 1, 1), EmploymentStatus.Active,
            null, null, null, true, null);

        _employeeRepository.GetByIdAsync(_employee.Id, Arg.Any<CancellationToken>()).Returns(_employee);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesAndReturnsSuccess()
    {
        var request = new CreateAttendanceRequest { EmployeeId = _employee.Id, AttendanceDate = AttendanceDate, Status = AttendanceStatus.Present };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EmployeeCode.Should().Be("EMP-1");
        await _repository.Received(1).AddAsync(Arg.Any<Attendance>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenEmployeeDoesNotExist_ReturnsInvalidEmployeeFailure()
    {
        _employeeRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Employee?)null);
        var request = new CreateAttendanceRequest { EmployeeId = Guid.NewGuid(), AttendanceDate = AttendanceDate };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.InvalidEmployee);
    }

    [Fact]
    public async Task CreateAsync_WhenRecordAlreadyExistsForEmployeeAndDate_ReturnsDuplicateAttendanceFailure()
    {
        _repository.ExistsForEmployeeAndDateAsync(_employee.Id, AttendanceDate, null, Arg.Any<CancellationToken>()).Returns(true);
        var request = new CreateAttendanceRequest { EmployeeId = _employee.Id, AttendanceDate = AttendanceDate };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.DuplicateAttendance);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Attendance>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckInAsync_WhenNoRowExistsForToday_CreatesRowWithPresentStatus()
    {
        _repository.GetByEmployeeAndDateAsync(_employee.Id, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns((Attendance?)null);
        var request = new CheckInRequest { EmployeeId = _employee.Id };

        var result = await _sut.CheckInAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(AttendanceStatus.Present);
        result.Value.CheckInTime.Should().NotBeNull();
        await _repository.Received(1).AddAsync(Arg.Any<Attendance>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckInAsync_WhenAlreadyCheckedIn_ReturnsAlreadyCheckedInFailure()
    {
        var existing = Attendance.Create(_employee.Id, DateOnly.FromDateTime(DateTime.UtcNow), DateTime.UtcNow, null, AttendanceStatus.Present, null, null);
        _repository.GetByEmployeeAndDateAsync(_employee.Id, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(existing);
        var request = new CheckInRequest { EmployeeId = _employee.Id };

        var result = await _sut.CheckInAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.AlreadyCheckedIn);
    }

    [Fact]
    public async Task CheckInAsync_WhenExistingRowHasNoCheckInTime_RecordsCheckInWithoutOverwritingStatus()
    {
        var existing = Attendance.Create(_employee.Id, DateOnly.FromDateTime(DateTime.UtcNow), null, null, AttendanceStatus.HalfDay, null, null);
        _repository.GetByEmployeeAndDateAsync(_employee.Id, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(existing);
        var request = new CheckInRequest { EmployeeId = _employee.Id };

        var result = await _sut.CheckInAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(AttendanceStatus.HalfDay);
        result.Value.CheckInTime.Should().NotBeNull();
        await _repository.DidNotReceive().AddAsync(Arg.Any<Attendance>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckInAsync_WhenEmployeeDoesNotExist_ReturnsInvalidEmployeeFailure()
    {
        _employeeRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var result = await _sut.CheckInAsync(new CheckInRequest { EmployeeId = Guid.NewGuid() }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.InvalidEmployee);
    }

    [Fact]
    public async Task CheckOutAsync_WhenNotCheckedIn_ReturnsNotCheckedInFailure()
    {
        _repository.GetByEmployeeAndDateAsync(_employee.Id, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns((Attendance?)null);

        var result = await _sut.CheckOutAsync(new CheckOutRequest { EmployeeId = _employee.Id }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.NotCheckedIn);
    }

    [Fact]
    public async Task CheckOutAsync_WhenAlreadyCheckedOut_ReturnsAlreadyCheckedOutFailure()
    {
        var existing = Attendance.Create(_employee.Id, DateOnly.FromDateTime(DateTime.UtcNow), DateTime.UtcNow, DateTime.UtcNow, AttendanceStatus.Present, null, null);
        _repository.GetByEmployeeAndDateAsync(_employee.Id, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _sut.CheckOutAsync(new CheckOutRequest { EmployeeId = _employee.Id }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.AlreadyCheckedOut);
    }

    [Fact]
    public async Task CheckOutAsync_WhenCheckedInAndNotYetCheckedOut_RecordsCheckOut()
    {
        var existing = Attendance.Create(_employee.Id, DateOnly.FromDateTime(DateTime.UtcNow), DateTime.UtcNow, null, AttendanceStatus.Present, null, null);
        _repository.GetByEmployeeAndDateAsync(_employee.Id, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _sut.CheckOutAsync(new CheckOutRequest { EmployeeId = _employee.Id }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CheckOutTime.Should().NotBeNull();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Attendance?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPagedResult()
    {
        var attendance = Attendance.Create(_employee.Id, AttendanceDate, null, null, AttendanceStatus.Present, null, null);
        _repository.GetPagedAsync(Arg.Any<AttendanceListQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<AttendanceWithEmployee> { new(attendance, _employee.EmployeeCode, "Ada Lovelace") }, 1));

        var result = await _sut.GetPagedAsync(new AttendanceListQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle(a => a.Id == attendance.Id && a.EmployeeName == "Ada Lovelace");
        result.TotalCount.Should().Be(1);
    }
}
