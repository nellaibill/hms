namespace HMS.Modules.IPD.Application.Abstractions;

/// <summary>
/// Generates the short, human-readable AdmissionNumber business identifier — distinct from
/// the entity's internal <c>Guid.CreateVersion7()</c> primary key. Implemented in
/// Infrastructure via a Postgres sequence, mirroring Patients' IPatientIdentifierGenerator.
/// </summary>
internal interface IAdmissionIdentifierGenerator
{
    Task<string> NextAdmissionNumberAsync(CancellationToken cancellationToken);
}
