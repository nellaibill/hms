using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

internal interface IDiagnosticPackageRepository
{
    Task AddAsync(DiagnosticPackage package, CancellationToken cancellationToken);

    /// <summary>Loads the package together with its Items — needed by every operation
    /// (including reads), since Items is this aggregate's whole reason for existing.</summary>
    Task<DiagnosticPackage?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string code, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<DiagnosticPackage> Items, int TotalCount)> GetPagedAsync(DiagnosticPackageListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
