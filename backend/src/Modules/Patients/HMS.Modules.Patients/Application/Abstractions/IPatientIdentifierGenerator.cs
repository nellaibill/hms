using HMS.Modules.Patients.Contracts;

namespace HMS.Modules.Patients.Application.Abstractions;

/// <summary>
/// Generates the short, human-readable business identifiers the UX spec requires (UHID,
/// registration number) — distinct from the entity's internal <c>Guid.CreateVersion7()</c>
/// primary key. Implemented in Infrastructure via Postgres sequences.
/// </summary>
internal interface IPatientIdentifierGenerator
{
    Task<string> NextUhidAsync(CancellationToken cancellationToken);

    Task<string> NextRegistrationNumberAsync(EncounterType encounterType, CancellationToken cancellationToken);
}
