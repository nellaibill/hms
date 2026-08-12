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

    /// <summary>Finds an existing, non-deleted patient with the same first+last name
    /// (case-insensitive) and a phone number that matches <paramref name="primaryPhone"/> once
    /// both are normalized to their last 10 digits — checked against the candidate's primary
    /// AND both alternate phone numbers, so a number that moved from "alternate" to "primary"
    /// (or vice versa) between two registrations still gets caught. Used by
    /// PatientService.CreateAsync to catch the same person being registered twice under two
    /// different UHIDs. Matches on phone+name together (not phone alone) so family members
    /// sharing a landline don't false-positive against each other.</summary>
    Task<Patient?> FindDuplicateAsync(string primaryPhone, string firstName, string lastName, CancellationToken cancellationToken);

    /// <summary>Reads the row's current optimistic-concurrency token (Postgres xmin) as
    /// tracked by this DbContext instance — reflects whatever was in the database as of the
    /// entity's last load/save within this request. See PatientService.UpdateAsync.</summary>
    string GetRowVersion(Patient patient);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
