using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;

namespace HMS.Modules.HR.Application.Abstractions;

internal interface ILeaveTypeRepository
{
    Task AddAsync(LeaveType leaveType, CancellationToken cancellationToken);

    Task<LeaveType?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string code, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<LeaveType> Items, int TotalCount)> GetPagedAsync(LeaveTypeListQuery query, CancellationToken cancellationToken);

    /// <summary>Every active leave type — backs the leave-balance calculation (one row per
    /// active leave type, per employee).</summary>
    Task<IReadOnlyList<LeaveType>> GetActiveAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
