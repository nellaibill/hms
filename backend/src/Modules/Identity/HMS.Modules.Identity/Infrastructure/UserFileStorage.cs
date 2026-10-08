using HMS.Modules.Identity.Application.Abstractions;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Identity.Infrastructure;

/// <summary>
/// Local-disk file storage under HMS.Api's wwwroot, mirroring
/// HMS.Modules.Patients.Infrastructure.PatientFileStorage — this app's established
/// file-upload pattern (see docs/DecisionLog.md's file-upload ADR). Unlike Patients'
/// history-preserving, freshly-GUID-named files, a profile photo is a single
/// always-replaceable slot per user, so the stored file is named after the user's own id
/// and a re-upload simply overwrites it in place. Stored under the caller's own tenant
/// folder: "uploads/Tenant/{tenantId}/users/{userId}{ext}" (see
/// <see cref="TenantFileLocations"/> and docs/DecisionLog.md ADR-087).
/// </summary>
internal class UserFileStorage : IUserFileStorage
{
    private readonly string _contentRootPath;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<UserFileStorage> _logger;

    public UserFileStorage(IHostEnvironment environment, ITenantContext tenantContext, ILogger<UserFileStorage> logger)
    {
        _contentRootPath = environment.ContentRootPath;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<string> SaveProfilePhotoAsync(Guid userId, string fileName, Stream content, CancellationToken cancellationToken)
    {
        var tenantId = CurrentTenantId();
        var directory = TenantFileLocations.PublicDirectory(_contentRootPath, tenantId, TenantFileLocations.Users);
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

        return TenantFileLocations.PublicRelativePath(tenantId, TenantFileLocations.Users, storedFileName);
    }

    public Task DeleteAsync(string? relativePath, CancellationToken cancellationToken)
    {
        var tenantId = CurrentTenantId();

        // Only ever this tenant's own users folder — a photo still at an older layout's path is
        // left for migrate-tenant-files rather than deleted from a request.
        if (!TenantFileLocations.IsInTenantPublicFolder(relativePath, tenantId, TenantFileLocations.Users))
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
            _logger.LogWarning(ex, "Could not delete replaced user photo {PhotoPath}", relativePath);
        }

        return Task.CompletedTask;
    }

    private Guid CurrentTenantId() => _tenantContext.TenantId
        ?? throw new InvalidOperationException("UserFileStorage reached without a resolved tenant.");
}
