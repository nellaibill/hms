using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;

namespace HMS.Modules.HR.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule — same split as IShiftAssignmentRepository.
/// </summary>
internal interface IShiftSwapRequestRepository
{
    Task AddAsync(ShiftSwapRequest shiftSwapRequest, CancellationToken cancellationToken);

    Task<ShiftSwapRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<ShiftSwapRequest> Items, int TotalCount)> GetPagedAsync(SwapRequestListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
