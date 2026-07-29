using HMS.Modules.Branding.Application.Abstractions;
using Microsoft.Extensions.Hosting;

namespace HMS.Modules.Branding.Infrastructure;

/// <summary>
/// Local-disk file storage under HMS.Api's wwwroot, mirroring
/// HMS.Modules.Patients.Infrastructure.PatientFileStorage — this app's established
/// file-upload pattern (see docs/DecisionLog.md's file-upload ADR). Single well-known slot
/// (no owning-entity id) since there is only ever one hospital logo per deployment.
/// </summary>
internal class BrandingLogoStorage : IBrandingLogoStorage
{
    private readonly string _rootPath;

    public BrandingLogoStorage(IHostEnvironment environment)
    {
        _rootPath = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads", "branding", "logo");
    }

    public async Task<string> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_rootPath);

        // Only the extension is taken from the caller-supplied file name — the stored
        // name itself is a fresh GUID, so a crafted file name can't traverse or overwrite
        // an unrelated path on disk.
        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{Guid.CreateVersion7()}{extension}";
        var fullPath = Path.Combine(_rootPath, storedFileName);

        await using (var fileStream = File.Create(fullPath))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        return Path.Combine("uploads", "branding", "logo", storedFileName).Replace('\\', '/');
    }
}
