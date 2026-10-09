using HMS.Modules.Documents.Application.Abstractions;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Hosting;

namespace HMS.Modules.Documents.Infrastructure;

/// <summary>
/// Local-disk file storage, mirroring HMS.Modules.Patients.Infrastructure.PatientFileStorage
/// (same "no premature complexity" rationale — no blob storage/CDN until there's a real
/// need). The one deliberate difference: this is stored under <c>App_Data</c>,
/// <em>outside</em> <c>wwwroot</c>, so it is structurally impossible for
/// `app.UseStaticFiles()` to ever serve a document's bytes directly — content only ever leaves
/// this module through DocumentsController's authenticated GET /{id}/content action (see
/// docs/ApiStandards.md §10's "served through a controlled download endpoint rather than
/// direct static file serving"). Every file sits in the caller's own tenant folder,
/// "App_Data/Tenant/{tenantId}/documents/{documentId}{ext}" (see
/// <see cref="TenantFileLocations"/> and docs/DecisionLog.md ADR-087).
/// </summary>
internal class DocumentFileStorage : IDocumentFileStorage
{
    private readonly string _contentRootPath;
    private readonly ITenantContext _tenantContext;

    public DocumentFileStorage(IHostEnvironment environment, ITenantContext tenantContext)
    {
        _contentRootPath = environment.ContentRootPath;
        _tenantContext = tenantContext;
    }

    public async Task<SavedFile> SaveAsync(Guid documentId, string fileName, Stream content, CancellationToken cancellationToken)
    {
        var tenantPath = TenantPath();
        Directory.CreateDirectory(tenantPath);

        // Only the extension is taken from the caller-supplied file name — the stored name
        // itself is the document's own id, so a crafted file name can't traverse or overwrite
        // an unrelated path on disk (mirrors PatientFileStorage's rationale).
        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{documentId}{extension}";
        var fullPath = Path.Combine(tenantPath, storedFileName);

        using var sha256 = System.Security.Cryptography.SHA256.Create();

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        await using (var fileStream = File.Create(fullPath))
        await using (var hashingStream = new System.Security.Cryptography.CryptoStream(fileStream, sha256, System.Security.Cryptography.CryptoStreamMode.Write, leaveOpen: true))
        {
            await content.CopyToAsync(hashingStream, cancellationToken);
        }

        var checksum = Convert.ToHexStringLower(sha256.Hash!);

        return new SavedFile(storedFileName, checksum);
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        var fullPath = Path.Combine(TenantPath(), storageKey);
        if (!File.Exists(fullPath))
        {
            // Not yet moved by migrate-tenant-files — read it where ADR-083 left it. Remove this
            // fallback once every host has been migrated.
            var previousPath = Path.Combine(PreviousTenantPath(), storageKey);
            if (File.Exists(previousPath))
            {
                fullPath = previousPath;
            }
        }

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        foreach (var fullPath in new[] { Path.Combine(TenantPath(), storageKey), Path.Combine(PreviousTenantPath(), storageKey) })
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        return Task.CompletedTask;
    }

    // storageKey itself never changes shape — it's still just "{documentId}{extension}" —
    // the tenant segment lives purely in the directory, resolved fresh on every call from
    // the caller's own request-scoped tenant rather than cached at construction time.
    private string TenantPath()
        => TenantFileLocations.PrivateDirectory(_contentRootPath, CurrentTenantId(), TenantFileLocations.Documents);

    // ADR-083's "App_Data/documents/{tenantId}" — only read and deleted from, never written to.
    private string PreviousTenantPath()
        => Path.Combine(_contentRootPath, "App_Data", "documents", CurrentTenantId().ToString());

    private Guid CurrentTenantId() => _tenantContext.TenantId
        ?? throw new InvalidOperationException("DocumentFileStorage reached without a resolved tenant.");
}
