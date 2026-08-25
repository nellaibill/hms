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

    public PatientDocumentOwnerExistenceChecker(IPatientRepository repository)
    {
        _repository = repository;
    }

    public DocumentOwnerType OwnerType => DocumentOwnerType.Patient;

    public Task<bool> ExistsAsync(Guid ownerId, CancellationToken cancellationToken)
        => _repository.ExistsAsync(ownerId, cancellationToken);
}
