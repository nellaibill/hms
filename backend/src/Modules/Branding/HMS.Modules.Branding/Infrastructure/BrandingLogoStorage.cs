using HMS.Modules.Branding.Application.Abstractions;
using HMS.Modules.Branding.Contracts;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Branding.Infrastructure;

/// <summary>
/// Local-disk file storage under HMS.Api's wwwroot, mirroring
/// HMS.Modules.Patients.Infrastructure.PatientFileStorage — this app's established
/// file-upload pattern (see docs/DecisionLog.md's file-upload ADR). One folder per logo slot
/// (no owning-entity id, since BrandingSettings is itself a one-row-per-tenant table — see
/// that entity's own doc comment), all under the caller's own tenant folder:
/// "uploads/Tenant/{tenantId}/branding/{slot}/{guid}{ext}" (see
/// <see cref="TenantFileLocations"/> and docs/DecisionLog.md ADR-087).
/// </summary>
internal class BrandingLogoStorage : IBrandingLogoStorage
{
    private readonly string _contentRootPath;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<BrandingLogoStorage> _logger;

    public BrandingLogoStorage(IHostEnvironment environment, ITenantContext tenantContext, ILogger<BrandingLogoStorage> logger)
    {
        _contentRootPath = environment.ContentRootPath;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<string> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken, string slot = BrandingLogoSlots.Primary)
    {
        var tenantId = CurrentTenantId();
        var directory = Path.Combine(TenantFileLocations.PublicDirectory(_contentRootPath, tenantId, TenantFileLocations.Branding), slot);
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

        return TenantFileLocations.PublicRelativePath(tenantId, TenantFileLocations.Branding, slot, storedFileName);
    }

    public Task DeleteAsync(string? relativePath, CancellationToken cancellationToken)
    {
        var tenantId = CurrentTenantId();

        // Only ever this tenant's own branding folder — a logo still at an older layout's path
        // is left for migrate-tenant-files rather than deleted from a request.
        if (!TenantFileLocations.IsInTenantPublicFolder(relativePath, tenantId, TenantFileLocations.Branding))
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
            // points at the new logo would not be.
            _logger.LogWarning(ex, "Could not delete replaced branding logo {LogoPath}", relativePath);
        }

        return Task.CompletedTask;
    }

    private Guid CurrentTenantId() => _tenantContext.TenantId
        ?? throw new InvalidOperationException("BrandingLogoStorage reached without a resolved tenant.");
}
