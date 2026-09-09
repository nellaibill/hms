using HMS.Modules.IPD.Domain;

namespace HMS.Modules.IPD.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule in docs/DeveloperHandbook.md — Application never references EF Core types.
/// </summary>
internal interface INursingAssessmentRepository
{
    Task AddAsync(NursingAssessment assessment, CancellationToken cancellationToken);

    Task<IReadOnlyList<NursingAssessment>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
