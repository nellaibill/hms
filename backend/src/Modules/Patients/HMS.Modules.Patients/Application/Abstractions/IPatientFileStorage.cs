namespace HMS.Modules.Patients.Application.Abstractions;

/// <summary>
/// Persists an uploaded file (photo, ID proof) and returns its stored relative path.
/// Implemented in Infrastructure as local disk storage under HMS.Api's wwwroot — the
/// simplest option consistent with docs/DeveloperHandbook.md's "no premature complexity"
/// principle; see docs/DecisionLog.md for the ADR recording this as the first file-upload
/// pattern in the app.
/// </summary>
internal interface IPatientFileStorage
{
    Task<string> SaveAsync(Guid patientId, string category, string fileName, Stream content, CancellationToken cancellationToken);
}
