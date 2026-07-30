namespace HMS.Modules.Products.Application.Abstractions;

/// <summary>
/// Persists an uploaded product image and returns its stored relative path. Implemented in
/// Infrastructure as local disk storage under HMS.Api's wwwroot, mirroring
/// HMS.Modules.Patients.Infrastructure.PatientFileStorage's per-entity-folder shape (a
/// product has many images, unlike Branding's single logo slot).
/// </summary>
internal interface IProductImageStorage
{
    Task<string> SaveAsync(Guid productId, string fileName, Stream content, CancellationToken cancellationToken);
}
