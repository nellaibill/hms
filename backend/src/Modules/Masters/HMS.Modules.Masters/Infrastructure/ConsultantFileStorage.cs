using HMS.Modules.Masters.Application.Abstractions;
using Microsoft.Extensions.Hosting;

namespace HMS.Modules.Masters.Infrastructure;

/// <summary>
/// Local-disk file storage under HMS.Api's wwwroot, mirroring
/// HMS.Modules.Identity.Infrastructure.UserFileStorage — a photo is a single always-replaceable
/// slot per consultant, so the stored file is named after the consultant's own id and a
/// re-upload simply overwrites it in place.
/// </summary>
internal class ConsultantFileStorage : IConsultantFileStorage
{
    private readonly string _rootPath;

    public ConsultantFileStorage(IHostEnvironment environment)
    {
        _rootPath = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads", "consultants");
    }

    public async Task<string> SavePhotoAsync(Guid consultantId, string fileName, Stream content, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_rootPath);

        // Only the extension is taken from the caller-supplied file name — the stored name
        // itself is the consultant's own id, so a crafted file name can't traverse or overwrite
        // an unrelated path on disk.
        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{consultantId}{extension}";
        var fullPath = Path.Combine(_rootPath, storedFileName);

        await using (var fileStream = File.Create(fullPath))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        return Path.Combine("uploads", "consultants", storedFileName).Replace('\\', '/');
    }
}
