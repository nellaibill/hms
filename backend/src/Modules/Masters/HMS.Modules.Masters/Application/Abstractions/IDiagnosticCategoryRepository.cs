using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

internal interface IDiagnosticCategoryRepository
{
    Task AddAsync(DiagnosticCategory category, CancellationToken cancellationToken);

    Task<DiagnosticCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string code, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<DiagnosticCategory> Items, int TotalCount)> GetPagedAsync(DiagnosticCategoryListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
