using HMS.Modules.Documents.Application;
using HMS.Modules.Documents.Contracts;
using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Contracts;

namespace HMS.Modules.HR.Application;

/// <summary>
/// Public (not internal): HrDashboardController — which ASP.NET Core requires to be a public
/// class with a public constructor for controller discovery/DI activation — takes this as a
/// constructor dependency (CS0051 otherwise).
/// </summary>
public interface IHrDashboardService
{
    Task<HrDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken);
}

/// <summary>
/// A handful of independent count/group-by queries, not a single mega-query — correctness and
/// readability over micro-optimization for a dashboard endpoint, per the HR MVP spec.
/// </summary>
internal class HrDashboardService : IHrDashboardService
{
    private const int ExpiringDocumentsWithinDays = 30;

    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IDocumentService _documentService;

    public HrDashboardService(
        IEmployeeRepository employeeRepository,
        IAttendanceRepository attendanceRepository,
        ILeaveRequestRepository leaveRequestRepository,
        IDocumentService documentService)
    {
        _employeeRepository = employeeRepository;
        _attendanceRepository = attendanceRepository;
        _leaveRequestRepository = leaveRequestRepository;
        _documentService = documentService;
    }

    public async Task<HrDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var totalEmployees = await _employeeRepository.CountAsync(cancellationToken);
        var activeEmployees = await _employeeRepository.CountActiveAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var statusCounts = await _attendanceRepository.GetStatusCountsForDateAsync(today, cancellationToken);

        // PresentToday folds Late/HalfDay into the headline count — see HrDashboardResponse's
        // own remarks.
        var presentToday = statusCounts.GetValueOrDefault(AttendanceStatus.Present)
            + statusCounts.GetValueOrDefault(AttendanceStatus.Late)
            + statusCounts.GetValueOrDefault(AttendanceStatus.HalfDay);
        var absentToday = statusCounts.GetValueOrDefault(AttendanceStatus.Absent);
        var onLeaveToday = statusCounts.GetValueOrDefault(AttendanceStatus.OnLeave);

        var pendingLeaveRequests = await _leaveRequestRepository.CountPendingAsync(cancellationToken);
        var expiringDocuments = await _documentService.GetExpiringDocumentCountAsync(DocumentOwnerType.Staff, ExpiringDocumentsWithinDays, cancellationToken);

        return new HrDashboardResponse
        {
            TotalEmployees = totalEmployees,
            ActiveEmployees = activeEmployees,
            PresentToday = presentToday,
            AbsentToday = absentToday,
            OnLeaveToday = onLeaveToday,
            PendingLeaveRequests = pendingLeaveRequests,
            ExpiringDocuments = expiringDocuments,
        };
    }
}
