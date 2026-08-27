using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.HR.Infrastructure.Repositories;

internal class LeaveRequestRepository : ILeaveRequestRepository
{
    private readonly HRDbContext _dbContext;

    public LeaveRequestRepository(HRDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken)
        => await _dbContext.LeaveRequests.AddAsync(leaveRequest, cancellationToken);

    public Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.LeaveRequests.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<LeaveRequestWithDetails> Items, int TotalCount)> GetPagedAsync(LeaveRequestListQuery query, CancellationToken cancellationToken)
    {
        var joined =
            from leaveRequest in _dbContext.LeaveRequests
            join employee in _dbContext.Employees on leaveRequest.EmployeeId equals employee.Id
            join leaveType in _dbContext.LeaveTypes on leaveRequest.LeaveTypeId equals leaveType.Id
            select new { leaveRequest, employee, leaveType };

        if (query.EmployeeId.HasValue)
        {
            joined = joined.Where(x => x.leaveRequest.EmployeeId == query.EmployeeId.Value);
        }

        if (query.LeaveTypeId.HasValue)
        {
            joined = joined.Where(x => x.leaveRequest.LeaveTypeId == query.LeaveTypeId.Value);
        }

        if (query.Status.HasValue)
        {
            joined = joined.Where(x => x.leaveRequest.Status == query.Status.Value);
        }

        if (query.DateFrom.HasValue)
        {
            joined = joined.Where(x => x.leaveRequest.StartDate >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            joined = joined.Where(x => x.leaveRequest.StartDate <= query.DateTo.Value);
        }

        joined = joined.OrderByDescending(x => x.leaveRequest.CreatedAt);

        var totalCount = await joined.CountAsync(cancellationToken);

        var page = await joined
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = page
            .Select(x => new LeaveRequestWithDetails(x.leaveRequest, x.employee.EmployeeCode, $"{x.employee.FirstName} {x.employee.LastName}", x.leaveType.Name))
            .ToList();

        return (items, totalCount);
    }

    public async Task<int> GetApprovedDaysAsync(Guid employeeId, Guid leaveTypeId, int year, CancellationToken cancellationToken)
        => await _dbContext.LeaveRequests
            .Where(l => l.EmployeeId == employeeId
                && l.LeaveTypeId == leaveTypeId
                && l.Status == LeaveRequestStatus.Approved
                && l.StartDate.Year == year)
            .SumAsync(l => (int?)l.TotalDays, cancellationToken) ?? 0;

    public Task<int> CountPendingAsync(CancellationToken cancellationToken)
        => _dbContext.LeaveRequests.CountAsync(l => l.Status == LeaveRequestStatus.Pending, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
