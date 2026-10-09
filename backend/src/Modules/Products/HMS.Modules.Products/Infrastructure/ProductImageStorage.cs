using HMS.Modules.Products.Application.Abstractions;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Hosting;

namespace HMS.Modules.Products.Infrastructure;

/// <summary>
/// Local-disk file storage under HMS.Api's wwwroot, mirroring
/// HMS.Modules.Patients.Infrastructure.PatientFileStorage — this app's established
/// file-upload pattern (see docs/DecisionLog.md's file-upload ADR). Stored under the caller's
/// own tenant folder: "uploads/Tenant/{tenantId}/products/{productId}/images/{guid}{ext}" (see
/// <see cref="TenantFileLocations"/> and docs/DecisionLog.md ADR-087).
/// </summary>
internal class ProductImageStorage : IProductImageStorage
{
    private readonly string _contentRootPath;
    private readonly ITenantContext _tenantContext;

    public ProductImageStorage(IHostEnvironment environment, ITenantContext tenantContext)
    {
        _contentRootPath = environment.ContentRootPath;
        _tenantContext = tenantContext;
    }

    public async Task<string> SaveAsync(Guid productId, string fileName, Stream content, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId
            ?? throw new InvalidOperationException("ProductImageStorage reached without a resolved tenant.");
        var directory = Path.Combine(
            TenantFileLocations.PublicDirectory(_contentRootPath, tenantId, TenantFileLocations.Products), productId.ToString(), "images");
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

        return TenantFileLocations.PublicRelativePath(tenantId, TenantFileLocations.Products, productId.ToString(), "images", storedFileName);
    }
}
