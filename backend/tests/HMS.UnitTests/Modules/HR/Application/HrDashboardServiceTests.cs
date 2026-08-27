using FluentAssertions;
using HMS.Modules.Documents.Application;
using HMS.Modules.Documents.Contracts;
using HMS.Modules.HR.Application;
using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Contracts;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.HR.Application;

public class HrDashboardServiceTests
{
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly IAttendanceRepository _attendanceRepository = Substitute.For<IAttendanceRepository>();
    private readonly ILeaveRequestRepository _leaveRequestRepository = Substitute.For<ILeaveRequestRepository>();
    private readonly IDocumentService _documentService = Substitute.For<IDocumentService>();
    private readonly HrDashboardService _sut;

    public HrDashboardServiceTests()
        => _sut = new HrDashboardService(_employeeRepository, _attendanceRepository, _leaveRequestRepository, _documentService);

    [Fact]
    public async Task GetDashboardAsync_FoldsLateAndHalfDayIntoPresentToday()
    {
        _employeeRepository.CountAsync(Arg.Any<CancellationToken>()).Returns(20);
        _employeeRepository.CountActiveAsync(Arg.Any<CancellationToken>()).Returns(18);
        _attendanceRepository.GetStatusCountsForDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<AttendanceStatus, int>
            {
                [AttendanceStatus.Present] = 10,
                [AttendanceStatus.Late] = 2,
                [AttendanceStatus.HalfDay] = 1,
                [AttendanceStatus.Absent] = 3,
                [AttendanceStatus.OnLeave] = 4,
            });
        _leaveRequestRepository.CountPendingAsync(Arg.Any<CancellationToken>()).Returns(5);
        _documentService.GetExpiringDocumentCountAsync(DocumentOwnerType.Staff, 30, Arg.Any<CancellationToken>()).Returns(2);

        var result = await _sut.GetDashboardAsync(CancellationToken.None);

        result.TotalEmployees.Should().Be(20);
        result.ActiveEmployees.Should().Be(18);
        result.PresentToday.Should().Be(13); // 10 + 2 + 1
        result.AbsentToday.Should().Be(3);
        result.OnLeaveToday.Should().Be(4);
        result.PendingLeaveRequests.Should().Be(5);
        result.ExpiringDocuments.Should().Be(2);
    }

    [Fact]
    public async Task GetDashboardAsync_WithNoAttendanceRowsToday_ReturnsAllZeros()
    {
        _attendanceRepository.GetStatusCountsForDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<AttendanceStatus, int>());

        var result = await _sut.GetDashboardAsync(CancellationToken.None);

        result.PresentToday.Should().Be(0);
        result.AbsentToday.Should().Be(0);
        result.OnLeaveToday.Should().Be(0);
    }
}
