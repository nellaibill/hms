using HMS.Modules.Masters.Application.Abstractions;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Hosting;

namespace HMS.Modules.Masters.Infrastructure;

/// <summary>
/// Local-disk file storage under HMS.Api's wwwroot, mirroring
/// HMS.Modules.Identity.Infrastructure.UserFileStorage — a photo is a single always-replaceable
/// slot per consultant, so the stored file is named after the consultant's own id and a
/// re-upload simply overwrites it in place. Scoped under the caller's own
/// <see cref="ITenantContext.TenantId"/> — this app is database-per-tenant but a single
/// shared filesystem/process, so without this every hospital's consultant photos would sit
/// in one shared directory tree (see docs/DecisionLog.md ADR-083). Stored tenant-first —
/// "uploads/Tenant/{tenantId}/consultants/…" — alongside every other upload kind's own
/// subfolder under that same tenant folder, rather than the kind-first
/// "uploads/consultants/{tenantId}/…" layout this replaced.
/// </summary>
internal class ConsultantFileStorage : IConsultantFileStorage
{
    private readonly string _rootPath;
    private readonly ITenantContext _tenantContext;

    public ConsultantFileStorage(IHostEnvironment environment, ITenantContext tenantContext)
    {
        _rootPath = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads", "Tenant");
        _tenantContext = tenantContext;
    }

    public async Task<string> SavePhotoAsync(Guid consultantId, string fileName, Stream content, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId
            ?? throw new InvalidOperationException("ConsultantFileStorage reached without a resolved tenant.");
        var directory = Path.Combine(_rootPath, tenantId.ToString(), "consultants");
        Directory.CreateDirectory(directory);

        // Only the extension is taken from the caller-supplied file name — the stored name
        // itself is the consultant's own id, so a crafted file name can't traverse or overwrite
        // an unrelated path on disk.
        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{consultantId}{extension}";
        var fullPath = Path.Combine(directory, storedFileName);

        await using (var fileStream = File.Create(fullPath))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        return Path.Combine("uploads", "Tenant", tenantId.ToString(), "consultants", storedFileName).Replace('\\', '/');
    }
}
