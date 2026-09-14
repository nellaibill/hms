namespace HMS.Modules.Masters.Application.Abstractions;

/// <summary>
/// Persists an uploaded consultant photo and returns its stored relative path. Implemented in
/// Infrastructure as local disk storage under HMS.Api's wwwroot, mirroring
/// HMS.Modules.Identity.Infrastructure.UserFileStorage — this app's established single-slot
/// photo pattern (one photo per record, uploaded/replaced after the record already exists).
/// </summary>
internal interface IConsultantFileStorage
{
    Task<string> SavePhotoAsync(Guid consultantId, string fileName, Stream content, CancellationToken cancellationToken);
}
