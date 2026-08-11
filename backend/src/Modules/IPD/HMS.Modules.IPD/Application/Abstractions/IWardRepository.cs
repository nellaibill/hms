using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;

namespace HMS.Modules.IPD.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule in docs/DeveloperHandbook.md — Application never references EF Core types.
/// </summary>
internal interface IWardRepository
{
    Task AddAsync(Ward ward, CancellationToken cancellationToken);

    Task<Ward?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string code, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Ward> Items, int TotalCount)> GetPagedAsync(WardListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
