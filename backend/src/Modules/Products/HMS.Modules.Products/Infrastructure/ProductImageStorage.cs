using HMS.Modules.Products.Application.Abstractions;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Hosting;

namespace HMS.Modules.Products.Infrastructure;

/// <summary>
/// Local-disk file storage under HMS.Api's wwwroot, mirroring
/// HMS.Modules.Patients.Infrastructure.PatientFileStorage — this app's established
/// file-upload pattern (see docs/DecisionLog.md's file-upload ADR). Scoped under the
/// caller's own <see cref="ITenantContext.TenantId"/> — this app is database-per-tenant but
/// a single shared filesystem/process, so without this every hospital's product images would
/// sit in one shared directory tree (see docs/DecisionLog.md ADR-083). Stored tenant-first —
/// "uploads/Tenant/{tenantId}/products/{productId}/images/…" — alongside every other upload
/// kind's own subfolder under that same tenant folder, rather than the kind-first
/// "uploads/products/{tenantId}/…" layout this replaced.
/// </summary>
internal class ProductImageStorage : IProductImageStorage
{
    private readonly string _rootPath;
    private readonly ITenantContext _tenantContext;

    public ProductImageStorage(IHostEnvironment environment, ITenantContext tenantContext)
    {
        _rootPath = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads", "Tenant");
        _tenantContext = tenantContext;
    }

    public async Task<string> SaveAsync(Guid productId, string fileName, Stream content, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId
            ?? throw new InvalidOperationException("ProductImageStorage reached without a resolved tenant.");
        var directory = Path.Combine(_rootPath, tenantId.ToString(), "products", productId.ToString(), "images");
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

        return Path.Combine("uploads", "Tenant", tenantId.ToString(), "products", productId.ToString(), "images", storedFileName).Replace('\\', '/');
    }
}
