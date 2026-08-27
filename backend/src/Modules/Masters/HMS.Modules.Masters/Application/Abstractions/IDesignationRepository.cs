using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

internal interface IDesignationRepository
{
    Task AddAsync(Designation designation, CancellationToken cancellationToken);

    Task<Designation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string code, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Designation> Items, int TotalCount)> GetPagedAsync(DesignationListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
