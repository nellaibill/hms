using System.IO.Compression;
using HMS.Modules.Backups.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Backups.Infrastructure;

/// <summary>
/// Zips one tenant's share of the five upload trees every file-storing module writes to (see
/// docs/DecisionLog.md ADR-083 — each is "&lt;root&gt;/{tenantId}/..." on the one shared
/// filesystem). The roots are repeated here rather than asked of each module, the same way
/// TenantFileStorageMigrator repeats them: the storage classes are internal to their own
/// modules, and only their root directory is needed, not their read/write behaviour. A new
/// upload kind needs adding here too, or its files silently won't be in the zip.
///
/// Inside the zip the tenant id segment is dropped and each root gets a readable folder name
/// ("documents/", "product-images/", …) — the layout below each tenant folder (e.g. a product's
/// "{productId}/images/") is kept as is.
/// </summary>
internal sealed class TenantFilesArchive : ITenantFilesArchive
{
    private readonly IReadOnlyList<FileSource> _sources;
    private readonly ILogger<TenantFilesArchive> _logger;

    public TenantFilesArchive(IHostEnvironment environment, ILogger<TenantFilesArchive> logger)
    {
        var uploadsRoot = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads");
        _sources =
        [
            new FileSource("documents", "Documents", Path.Combine(environment.ContentRootPath, "App_Data", "documents")),
            new FileSource("consultant-photos", "Consultant photos", Path.Combine(uploadsRoot, "consultants")),
            new FileSource("user-photos", "User photos", Path.Combine(uploadsRoot, "users")),
            new FileSource("product-images", "Product images", Path.Combine(uploadsRoot, "products")),
            new FileSource("branding", "Branding", Path.Combine(uploadsRoot, "branding")),
        ];
        _logger = logger;
    }

    public Task<TenantFilesSummary> GetSummaryAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var categories = _sources
            .Select(source =>
            {
                var files = EnumerateFiles(source.TenantRoot(tenantId)).ToList();
                return new TenantFileCategory(source.Key, source.Label, files.Count, files.Sum(file => file.Length));
            })
            .ToList();

        return Task.FromResult(new TenantFilesSummary(categories));
    }

    public async Task<int> WriteZipAsync(Guid tenantId, Stream destination, CancellationToken cancellationToken)
    {
        var written = 0;

        using (var archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var source in _sources)
            {
                var tenantRoot = source.TenantRoot(tenantId);
                foreach (var file in EnumerateFiles(tenantRoot))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Opened before the entry is created, so a file that vanished (a document
                    // deleted mid-download) or is still being written by an upload is skipped
                    // cleanly instead of leaving an empty entry behind or failing the whole zip.
                    // Shared for write/delete too, so a slow download never blocks a user from
                    // replacing a photo or deleting a document at the same time.
                    FileStream input;
                    try
                    {
                        input = new FileStream(
                            file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 81920, useAsync: true);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        _logger.LogWarning(ex, "Skipped {FilePath} while zipping tenant {TenantId}'s files — it could not be opened.", file.FullName, tenantId);
                        continue;
                    }

                    await using (input)
                    {
                        var relativePath = Path.GetRelativePath(tenantRoot, file.FullName).Replace('\\', '/');

                        // Fastest, not Optimal: nearly everything here is a JPEG/PNG/PDF that is
                        // already compressed, so harder compression buys almost nothing.
                        var entry = archive.CreateEntry($"{source.Key}/{relativePath}", CompressionLevel.Fastest);
                        entry.LastWriteTime = file.LastWriteTime;

                        await using var output = entry.Open();
                        await input.CopyToAsync(output, cancellationToken);
                    }

                    written++;
                }
            }
        }

        return written;
    }

    private static IEnumerable<FileInfo> EnumerateFiles(string directory)
        => Directory.Exists(directory)
            ? new DirectoryInfo(directory)
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .OrderBy(file => file.FullName, StringComparer.Ordinal)
            : [];

    private sealed record FileSource(string Key, string Label, string Root)
    {
        public string TenantRoot(Guid tenantId) => Path.Combine(Root, tenantId.ToString());
    }
}
