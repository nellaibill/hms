using HMS.Modules.Patients.Application.Abstractions;
using Microsoft.Extensions.Hosting;

namespace HMS.Modules.Patients.Infrastructure;

/// <summary>
/// Local-disk file storage under HMS.Api's wwwroot — the simplest option consistent with
/// docs/DeveloperHandbook.md's "no premature complexity" principle (no blob storage/CDN
/// until there's a real need). This is the app's first file-upload path; see
/// docs/DecisionLog.md for the ADR recording the decision.
/// </summary>
internal class PatientFileStorage : IPatientFileStorage
{
    private readonly string _rootPath;

    public PatientFileStorage(IHostEnvironment environment)
    {
        _rootPath = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads", "patients");
    }

    public async Task<string> SaveAsync(Guid patientId, string category, string fileName, Stream content, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(_rootPath, patientId.ToString(), category);
        Directory.CreateDirectory(directory);

        // Only the extension is taken from the caller-supplied file name — the stored
        // name itself is a fresh GUID, so a crafted file name can't traverse or overwrite
        // an unrelated path on disk.
        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{Guid.CreateVersion7()}{extension}";
        var fullPath = Path.Combine(directory, storedFileName);

        await using (var fileStream = File.Create(fullPath))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        return Path.Combine("uploads", "patients", patientId.ToString(), category, storedFileName).Replace('\\', '/');
    }
}
