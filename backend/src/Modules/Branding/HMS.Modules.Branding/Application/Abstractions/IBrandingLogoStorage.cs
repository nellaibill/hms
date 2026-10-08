namespace HMS.Modules.Branding.Application.Abstractions;

/// <summary>
/// Persists an uploaded hospital logo and returns its stored relative path. Implemented in
/// Infrastructure as local disk storage under HMS.Api's wwwroot, mirroring
/// HMS.Modules.Patients.Infrastructure.PatientFileStorage — this app's established
/// file-upload pattern. Unlike patient files, there is only ever one logo per slot, so no
/// owning-entity id is threaded through.
/// </summary>
internal interface IBrandingLogoStorage
{
    /// <param name="slot">A Contracts.BrandingLogoSlots value — each slot gets its own folder.</param>
    Task<string> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken, string slot = Contracts.BrandingLogoSlots.Primary);

    /// <summary>Deletes a logo this storage saved earlier (a replaced or removed slot). A null
    /// path, or one outside the current tenant's branding folder, is ignored.</summary>
    Task DeleteAsync(string? relativePath, CancellationToken cancellationToken);
}
