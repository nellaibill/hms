using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;

namespace HMS.Modules.Patients.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule in docs/DeveloperHandbook.md §4 — Application never references EF Core types.
/// </summary>
internal interface IPatientRepository
{
    Task AddAsync(Patient patient, CancellationToken cancellationToken);

    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Lighter-weight than GetByIdAsync for callers that only need to know a patient
    /// exists — added for PatientDocumentOwnerExistenceChecker (see Infrastructure/).</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Patient> Items, int TotalCount)> GetPagedAsync(PatientListQuery query, CancellationToken cancellationToken);

    /// <summary>Finds an existing, non-deleted patient with the same primary phone number
    /// and the same first+last name (case-insensitive) — used by PatientService.CreateAsync
    /// to catch the same person being registered twice under two different UHIDs. Matches on
    /// phone+name together (not phone alone) so family members sharing a landline don't
    /// false-positive against each other.</summary>
    Task<Patient?> FindDuplicateAsync(string primaryPhone, string firstName, string lastName, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
