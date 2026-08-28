using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

internal interface IDiagnosticTestRepository
{
    Task AddAsync(DiagnosticTest diagnosticTest, CancellationToken cancellationToken);

    Task<DiagnosticTest?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(string name, DiagnosticTestServiceType serviceType, bool isOutsourced, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<DiagnosticTest> Items, int TotalCount)> GetPagedAsync(DiagnosticTestListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
