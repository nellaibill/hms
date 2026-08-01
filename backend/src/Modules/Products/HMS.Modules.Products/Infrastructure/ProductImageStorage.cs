using HMS.Modules.Products.Application.Abstractions;
using Microsoft.Extensions.Hosting;

namespace HMS.Modules.Products.Infrastructure;

/// <summary>
/// Local-disk file storage under HMS.Api's wwwroot, mirroring
/// HMS.Modules.Patients.Infrastructure.PatientFileStorage — this app's established
/// file-upload pattern (see docs/DecisionLog.md's file-upload ADR).
/// </summary>
internal class ProductImageStorage : IProductImageStorage
{
    private readonly string _rootPath;

    public ProductImageStorage(IHostEnvironment environment)
    {
        _rootPath = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads", "products");
    }

    public async Task<string> SaveAsync(Guid productId, string fileName, Stream content, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(_rootPath, productId.ToString(), "images");
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

        return Path.Combine("uploads", "products", productId.ToString(), "images", storedFileName).Replace('\\', '/');
    }
}
