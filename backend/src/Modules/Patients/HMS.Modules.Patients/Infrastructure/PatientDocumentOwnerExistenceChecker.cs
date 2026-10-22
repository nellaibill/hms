using HMS.Modules.Documents.Application.Abstractions;
using HMS.Modules.Documents.Contracts;
using HMS.Modules.Patients.Application.Abstractions;

namespace HMS.Modules.Patients.Infrastructure;

/// <summary>
/// Lets HMS.Modules.Documents validate that a Patient owner id actually exists before
/// accepting an upload against it — see IDocumentOwnerExistenceChecker's remarks for why
/// this is a one-directional dependency (Patients → Documents) rather than the other way
/// around.
/// </summary>
internal class PatientDocumentOwnerExistenceChecker : IDocumentOwnerExistenceChecker
{
    private readonly IPatientRepository _repository;
    private readonly IPatientVisitRepository _visitRepository;

    public PatientDocumentOwnerExistenceChecker(IPatientRepository repository, IPatientVisitRepository visitRepository)
    {
        _repository = repository;
        _visitRepository = visitRepository;
    }

    public DocumentOwnerType OwnerType => DocumentOwnerType.Patient;

    public Task<bool> ExistsAsync(Guid ownerId, CancellationToken cancellationToken)
        => _repository.ExistsAsync(ownerId, cancellationToken);

    /// <summary>A patient document can be filed under one of that patient's own visits.</summary>
    public async Task<bool> HasVisitAsync(Guid ownerId, Guid visitId, CancellationToken cancellationToken)
        => (await _visitRepository.GetByIdAsync(visitId, cancellationToken))?.PatientId == ownerId;
}
