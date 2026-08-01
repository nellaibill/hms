using HMS.Modules.Identity.Application.Abstractions;
using Microsoft.Extensions.Hosting;

namespace HMS.Modules.Identity.Infrastructure;

/// <summary>
/// Local-disk file storage under HMS.Api's wwwroot, mirroring
/// HMS.Modules.Patients.Infrastructure.PatientFileStorage — this app's established
/// file-upload pattern (see docs/DecisionLog.md's file-upload ADR). Unlike Patients'
/// history-preserving, freshly-GUID-named files, a profile photo is a single
/// always-replaceable slot per user, so the stored file is named after the user's own id
/// and a re-upload simply overwrites it in place.
/// </summary>
internal class UserFileStorage : IUserFileStorage
{
    private readonly string _rootPath;

    public UserFileStorage(IHostEnvironment environment)
    {
        _rootPath = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads", "users");
    }

    public async Task<string> SaveProfilePhotoAsync(Guid userId, string fileName, Stream content, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_rootPath);

        // Only the extension is taken from the caller-supplied file name — the stored name
        // itself is the user's own id, so a crafted file name can't traverse or overwrite
        // an unrelated path on disk.
        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{userId}{extension}";
        var fullPath = Path.Combine(_rootPath, storedFileName);

        await using (var fileStream = File.Create(fullPath))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        return Path.Combine("uploads", "users", storedFileName).Replace('\\', '/');
    }
}
