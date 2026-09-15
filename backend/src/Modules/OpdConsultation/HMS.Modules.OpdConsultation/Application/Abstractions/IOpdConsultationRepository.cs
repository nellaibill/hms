namespace HMS.Modules.OpdConsultation.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency inversion
/// rule in docs/Architecture.md — Application never references EF Core types.
/// </summary>
internal interface IOpdConsultationRepository
{
    Task AddAsync(Domain.OpdConsultationNote note, CancellationToken cancellationToken);

    Task<Domain.OpdConsultationNote?> GetByConsultationIdAsync(Guid consultationId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
