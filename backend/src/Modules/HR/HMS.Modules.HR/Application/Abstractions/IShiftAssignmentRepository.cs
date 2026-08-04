using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;

namespace HMS.Modules.HR.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule — same split as IShiftRepository.
/// </summary>
internal interface IShiftAssignmentRepository
{
    Task AddAsync(ShiftAssignment shiftAssignment, CancellationToken cancellationToken);

    Task<ShiftAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<ShiftAssignment> Items, int TotalCount)> GetPagedAsync(ShiftAssignmentListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
