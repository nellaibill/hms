namespace HMS.Modules.DischargeSummary.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule in docs/Architecture.md — Application never references EF Core types.
/// </summary>
internal interface IDischargeSummaryRepository
{
    Task AddAsync(Domain.DischargeSummary summary, CancellationToken cancellationToken);

    Task<Domain.DischargeSummary?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Domain.DischargeSummary?> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
