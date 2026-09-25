using HMS.Modules.Branding.Application.Abstractions;
using HMS.Modules.Branding.Contracts;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Hosting;

namespace HMS.Modules.Branding.Infrastructure;

/// <summary>
/// Local-disk file storage under HMS.Api's wwwroot, mirroring
/// HMS.Modules.Patients.Infrastructure.PatientFileStorage — this app's established
/// file-upload pattern (see docs/DecisionLog.md's file-upload ADR). Single well-known slot
/// per tenant (no owning-entity id, since BrandingSettings is itself a one-row-per-tenant
/// table — see that entity's own doc comment) — but this app is database-per-tenant with a
/// single shared filesystem/process underneath, so the slot is scoped under the caller's own
/// <see cref="ITenantContext.TenantId"/> rather than shared across every hospital (see
/// docs/DecisionLog.md ADR-083).
/// </summary>
internal class BrandingLogoStorage : IBrandingLogoStorage
{
    private readonly string _rootPath;
    private readonly ITenantContext _tenantContext;

    public BrandingLogoStorage(IHostEnvironment environment, ITenantContext tenantContext)
    {
        _rootPath = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads", "branding");
        _tenantContext = tenantContext;
    }

    public async Task<string> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken, string slot = BrandingLogoSlots.Primary)
    {
        // Primary keeps the original "logo" folder so existing stored paths stay where they are.
        var folder = slot == BrandingLogoSlots.Primary ? "logo" : slot;
        var tenantId = _tenantContext.TenantId
            ?? throw new InvalidOperationException("BrandingLogoStorage reached without a resolved tenant.");
        var directory = Path.Combine(_rootPath, tenantId.ToString(), folder);
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

        return Path.Combine("uploads", "branding", tenantId.ToString(), folder, storedFileName).Replace('\\', '/');
    }
}
