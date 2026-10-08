using HMS.Modules.Masters.Application.Abstractions;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Masters.Infrastructure;

/// <summary>
/// Local-disk file storage under HMS.Api's wwwroot, mirroring
/// HMS.Modules.Identity.Infrastructure.UserFileStorage — a photo is a single always-replaceable
/// slot per consultant, so the stored file is named after the consultant's own id and a
/// re-upload simply overwrites it in place. Stored under the caller's own tenant folder:
/// "uploads/Tenant/{tenantId}/consultants/{consultantId}{ext}" (see
/// <see cref="TenantFileLocations"/> and docs/DecisionLog.md ADR-087).
/// </summary>
internal class ConsultantFileStorage : IConsultantFileStorage
{
    private readonly string _contentRootPath;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ConsultantFileStorage> _logger;

    public ConsultantFileStorage(IHostEnvironment environment, ITenantContext tenantContext, ILogger<ConsultantFileStorage> logger)
    {
        _contentRootPath = environment.ContentRootPath;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<string> SavePhotoAsync(Guid consultantId, string fileName, Stream content, CancellationToken cancellationToken)
    {
        var tenantId = CurrentTenantId();
        var directory = TenantFileLocations.PublicDirectory(_contentRootPath, tenantId, TenantFileLocations.Consultants);
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

        return TenantFileLocations.PublicRelativePath(tenantId, TenantFileLocations.Consultants, storedFileName);
    }

    public Task DeleteAsync(string? relativePath, CancellationToken cancellationToken)
    {
        var tenantId = CurrentTenantId();

        // Only ever this tenant's own consultants folder — a photo still at an older layout's
        // path is left for migrate-tenant-files rather than deleted from a request.
        if (!TenantFileLocations.IsInTenantPublicFolder(relativePath, tenantId, TenantFileLocations.Consultants))
        {
            return Task.CompletedTask;
        }

        try
        {
            File.Delete(TenantFileLocations.PublicFullPath(_contentRootPath, relativePath!));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A leftover file is harmless; failing the request after the database already
            // points at the new photo would not be.
            _logger.LogWarning(ex, "Could not delete replaced consultant photo {PhotoPath}", relativePath);
        }

        return Task.CompletedTask;
    }

    private Guid CurrentTenantId() => _tenantContext.TenantId
        ?? throw new InvalidOperationException("ConsultantFileStorage reached without a resolved tenant.");
}
