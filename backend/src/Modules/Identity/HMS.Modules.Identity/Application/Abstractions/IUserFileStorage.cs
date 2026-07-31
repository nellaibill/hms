namespace HMS.Modules.Identity.Application.Abstractions;

/// <summary>
/// Persists an uploaded user profile photo and returns its stored relative path.
/// Implemented in Infrastructure as local disk storage under HMS.Api's wwwroot, mirroring
/// HMS.Modules.Patients.Infrastructure.PatientFileStorage — this app's established
/// file-upload pattern (see docs/DecisionLog.md's file-upload ADR). Only one photo per
/// user (no category parameter, unlike Patients' photo/ID-proof split), so the shape
/// stays as small as Branding's single-slot storage.
/// </summary>
internal interface IUserFileStorage
{
    Task<string> SaveProfilePhotoAsync(Guid userId, string fileName, Stream content, CancellationToken cancellationToken);
}
