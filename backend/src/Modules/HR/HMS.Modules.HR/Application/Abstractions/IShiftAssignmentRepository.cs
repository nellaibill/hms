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

    // Backs the double-booking check in ShiftAssignmentService: every other assignment
    // the same staff member has on the same roster date, so their shift times can be
    // compared for overlap.
    Task<IReadOnlyList<ShiftAssignment>> GetByStaffAndDateAsync(Guid staffId, DateOnly rosterDate, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<ShiftAssignment> Items, int TotalCount)> GetPagedAsync(ShiftAssignmentListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
