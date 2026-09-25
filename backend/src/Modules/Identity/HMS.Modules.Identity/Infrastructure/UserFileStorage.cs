using HMS.Modules.Identity.Application.Abstractions;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Hosting;

namespace HMS.Modules.Identity.Infrastructure;

/// <summary>
/// Local-disk file storage under HMS.Api's wwwroot, mirroring
/// HMS.Modules.Patients.Infrastructure.PatientFileStorage — this app's established
/// file-upload pattern (see docs/DecisionLog.md's file-upload ADR). Unlike Patients'
/// history-preserving, freshly-GUID-named files, a profile photo is a single
/// always-replaceable slot per user, so the stored file is named after the user's own id
/// and a re-upload simply overwrites it in place. Scoped under the caller's own
/// <see cref="ITenantContext.TenantId"/> — this app is database-per-tenant but a single
/// shared filesystem/process, so without this every hospital's user photos would sit in one
/// shared directory tree (see docs/DecisionLog.md ADR-083). Stored tenant-first —
/// "uploads/Tenant/{tenantId}/users/…" — alongside every other upload kind's own subfolder
/// under that same tenant folder, rather than the kind-first "uploads/users/{tenantId}/…"
/// layout this replaced.
/// </summary>
internal class UserFileStorage : IUserFileStorage
{
    private readonly string _rootPath;
    private readonly ITenantContext _tenantContext;

    public UserFileStorage(IHostEnvironment environment, ITenantContext tenantContext)
    {
        _rootPath = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads", "Tenant");
        _tenantContext = tenantContext;
    }

    public async Task<string> SaveProfilePhotoAsync(Guid userId, string fileName, Stream content, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId
            ?? throw new InvalidOperationException("UserFileStorage reached without a resolved tenant.");
        var directory = Path.Combine(_rootPath, tenantId.ToString(), "users");
        Directory.CreateDirectory(directory);

        // Only the extension is taken from the caller-supplied file name — the stored name
        // itself is the user's own id, so a crafted file name can't traverse or overwrite
        // an unrelated path on disk.
        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{userId}{extension}";
        var fullPath = Path.Combine(directory, storedFileName);

        await using (var fileStream = File.Create(fullPath))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        return Path.Combine("uploads", "Tenant", tenantId.ToString(), "users", storedFileName).Replace('\\', '/');
    }
}
