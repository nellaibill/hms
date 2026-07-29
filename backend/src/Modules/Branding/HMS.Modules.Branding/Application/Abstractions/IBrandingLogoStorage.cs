namespace HMS.Modules.Branding.Application.Abstractions;

/// <summary>
/// Persists an uploaded hospital logo and returns its stored relative path. Implemented in
/// Infrastructure as local disk storage under HMS.Api's wwwroot, mirroring
/// HMS.Modules.Patients.Infrastructure.PatientFileStorage — this app's established
/// file-upload pattern. Unlike patient files, there is only ever one logo (single-tenant
/// deployment), so no owning-entity id is threaded through.
/// </summary>
internal interface IBrandingLogoStorage
{
    Task<string> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken);
}
