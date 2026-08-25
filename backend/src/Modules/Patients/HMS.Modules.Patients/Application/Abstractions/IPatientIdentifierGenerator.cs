namespace HMS.Modules.Patients.Application.Abstractions;

/// <summary>
/// Generates the short, human-readable UHID business identifier — distinct from the
/// entity's internal <c>Guid.CreateVersion7()</c> primary key. Implemented in Infrastructure
/// via a Postgres sequence.
/// </summary>
internal interface IPatientIdentifierGenerator
{
    Task<string> NextUhidAsync(CancellationToken cancellationToken);
}
