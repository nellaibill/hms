using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;

namespace HMS.Modules.Patients.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule — Application never references EF Core types.
/// </summary>
internal interface IPatientRepository
{
    Task AddAsync(Patient patient, CancellationToken cancellationToken);

    /// <summary>Loads the full aggregate (Address, Allergies, EmergencyContacts included).</summary>
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Lighter-weight than GetByIdAsync for callers that only need to know a patient
    /// exists — used by PatientDocumentOwnerExistenceChecker.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Patient> Items, int TotalCount)> GetPagedAsync(PatientListQuery query, CancellationToken cancellationToken);

    /// <summary>Finds an existing, non-deleted patient matching on primary phone + name
    /// (case-insensitive) always, and additionally on IdProofNumber when one is supplied —
    /// used by PatientService.CreateAsync to catch the same person being registered twice.</summary>
    Task<Patient?> FindDuplicateAsync(string primaryPhone, string firstName, string lastName, string? idProofNumber, CancellationToken cancellationToken);

    /// <summary>Reads the row's current optimistic-concurrency token (Postgres xmin) as
    /// tracked by this DbContext instance. See PatientService.UpdateAsync.</summary>
    string GetRowVersion(Patient patient);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
