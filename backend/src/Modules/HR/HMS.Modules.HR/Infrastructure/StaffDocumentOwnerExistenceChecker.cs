using HMS.Modules.Documents.Application.Abstractions;
using HMS.Modules.Documents.Contracts;
using HMS.Modules.HR.Application.Abstractions;

namespace HMS.Modules.HR.Infrastructure;

/// <summary>
/// Lets HMS.Modules.Documents validate that a Staff (Employee) owner id actually exists
/// before accepting an upload against it — see IDocumentOwnerExistenceChecker's remarks for
/// why this is a one-directional dependency (HR → Documents) rather than the other way
/// around, mirroring HMS.Modules.Patients.Infrastructure.PatientDocumentOwnerExistenceChecker.
/// Confirmed via docs/DecisionLog.md ADR-036: no existence checker was previously registered
/// for DocumentOwnerType.Staff, so uploads against it were never existence-validated before
/// this module's Employee entity existed.
/// </summary>
internal class StaffDocumentOwnerExistenceChecker : IDocumentOwnerExistenceChecker
{
    private readonly IEmployeeRepository _repository;

    public StaffDocumentOwnerExistenceChecker(IEmployeeRepository repository)
    {
        _repository = repository;
    }

    public DocumentOwnerType OwnerType => DocumentOwnerType.Staff;

    public Task<bool> ExistsAsync(Guid ownerId, CancellationToken cancellationToken)
        => _repository.ExistsAsync(ownerId, cancellationToken);
}
