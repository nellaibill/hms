using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

internal interface IDiagnosisRepository
{
    Task AddAsync(Diagnosis diagnosis, CancellationToken cancellationToken);

    Task<Diagnosis?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(string name, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Diagnosis> Items, int TotalCount)> GetPagedAsync(DiagnosisListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
