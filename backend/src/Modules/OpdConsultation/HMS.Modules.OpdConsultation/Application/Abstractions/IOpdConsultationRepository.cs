namespace HMS.Modules.OpdConsultation.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency inversion
/// rule in docs/Architecture.md — Application never references EF Core types.
/// </summary>
internal interface IOpdConsultationRepository
{
    Task AddAsync(Domain.OpdConsultationNote note, CancellationToken cancellationToken);

    Task<Domain.OpdConsultationNote?> GetByConsultationIdAsync(Guid consultationId, CancellationToken cancellationToken);

    /// <summary>Every note already on file for one patient (read-only, untracked), newest first.</summary>
    Task<IReadOnlyList<Domain.OpdConsultationNote>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken);

    /// <summary>Every note (Draft or Completed) recorded against one visit — backs OPD Billing
    /// Entry's pre-fill of the doctor's catalog investigations (regression report OPD-01).</summary>
    Task<IReadOnlyList<Domain.OpdConsultationNote>> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
