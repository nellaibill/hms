using System.IO.Compression;
using HMS.Modules.Backups.Application.Abstractions;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Backups.Infrastructure;

/// <summary>
/// Zips one tenant's five upload kinds, each a subfolder of that tenant's own folder (see
/// <see cref="TenantFileLocations"/> and docs/DecisionLog.md ADR-087). The kinds are listed
/// here rather than asked of each module: the storage classes are internal to their own
/// modules, and only their folder is needed, not their read/write behaviour. A new upload kind
/// needs adding here too, or its files silently won't be in the zip.
///
/// Inside the zip each kind gets a readable folder name ("documents/", "product-images/", …) —
/// the layout below each kind's folder (e.g. a product's "{productId}/images/") is kept as is.
/// </summary>
internal sealed class TenantFilesArchive : ITenantFilesArchive
{
    // Every upload kind's allowed type that is already compressed internally. Deflating these
    // again only makes them bigger (seen live: PNG user photos grew ~5% in the zip), so they
    // are stored as-is. Anything else (SVG logos, in practice) is still deflated.
    private static readonly HashSet<string> AlreadyCompressedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".pdf", ".docx", ".xlsx",
    };

    private readonly IReadOnlyList<FileSource> _sources;
    private readonly ILogger<TenantFilesArchive> _logger;

    public TenantFilesArchive(IHostEnvironment environment, ILogger<TenantFilesArchive> logger)
    {
        var contentRoot = environment.ContentRootPath;
        _sources =
        [
            new FileSource("documents", "Documents", tenantId => TenantFileLocations.PrivateDirectory(contentRoot, tenantId, TenantFileLocations.Documents)),
            new FileSource("consultant-photos", "Consultant photos", tenantId => TenantFileLocations.PublicDirectory(contentRoot, tenantId, TenantFileLocations.Consultants)),
            new FileSource("user-photos", "User photos", tenantId => TenantFileLocations.PublicDirectory(contentRoot, tenantId, TenantFileLocations.Users)),
            new FileSource("product-images", "Product images", tenantId => TenantFileLocations.PublicDirectory(contentRoot, tenantId, TenantFileLocations.Products)),
            new FileSource("branding", "Branding", tenantId => TenantFileLocations.PublicDirectory(contentRoot, tenantId, TenantFileLocations.Branding)),
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

                        var compression = AlreadyCompressedExtensions.Contains(file.Extension)
                            ? CompressionLevel.NoCompression
                            : CompressionLevel.Optimal;
                        var entry = archive.CreateEntry($"{source.Key}/{relativePath}", compression);
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

    private sealed record FileSource(string Key, string Label, Func<Guid, string> TenantRoot);
}
