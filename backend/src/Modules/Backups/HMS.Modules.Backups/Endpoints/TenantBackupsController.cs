using HMS.Modules.Backups.Application.Abstractions;
using HMS.Modules.Backups.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Backups.Endpoints;

/// <summary>
/// Each hospital's own view of its own backup — never any other tenant's, and never masters.
/// "Which tenant" comes entirely from the caller's own Hospital JWT via ITenantContext, the
/// same tenant-resolution seam every other per-tenant module already relies on — there is no
/// request parameter naming a tenant, so there is nothing here for a caller to manipulate to
/// reach another hospital's data. Reuses the existing identity-administration.view permission
/// (already gates the whole Settings area) rather than adding a new one. Also serves the
/// hospital's uploaded documents &amp; images as a zip (see ITenantFilesArchive) — the files a
/// database dump only holds the paths of.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/backups")]
public class TenantBackupsController : ControllerBase
{
    private readonly IBackupStorage _storage;
    private readonly ITenantFilesArchive _filesArchive;
    private readonly ITenantContext _tenantContext;

    public TenantBackupsController(IBackupStorage storage, ITenantFilesArchive filesArchive, ITenantContext tenantContext)
    {
        _storage = storage;
        _filesArchive = filesArchive;
        _tenantContext = tenantContext;
    }

    /// <summary>The current tenant's own latest backup, if any.</summary>
    /// <response code="200">The backup's metadata (possibly unavailable).</response>
    [RequirePermission("identity-administration.view")]
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var latest = await _storage.GetLatestAsync(CurrentTenantKey(), cancellationToken);
        return Ok(new ApiResponse<BackupSummaryResponse>
        {
            Data = new BackupSummaryResponse
            {
                Key = CurrentTenantKey(),
                Label = "This hospital",
                LastBackupDate = latest?.Date,
                SizeBytes = latest?.SizeBytes,
                IsAvailable = latest is not null,
            },
        });
    }

    /// <summary>Streams the current tenant's own latest backup.</summary>
    /// <response code="200">The backup file.</response>
    /// <response code="404">No backup is available yet for this hospital.</response>
    [RequirePermission("identity-administration.view")]
    [HttpGet("mine/download")]
    public async Task<IActionResult> DownloadMine(CancellationToken cancellationToken)
    {
        var latest = await _storage.GetLatestAsync(CurrentTenantKey(), cancellationToken);
        if (latest is null)
        {
            return NotFound(new ApiErrorResponse
            {
                ErrorCode = "BACKUPS.NOT_FOUND",
                Message = "No backup is available yet for this hospital.",
                CorrelationId = HttpContext.GetCorrelationId(),
                Timestamp = DateTime.UtcNow,
            });
        }

        var downloadName = $"backup-{latest.Date:yyyy-MM-dd}.dump";
        return PhysicalFile(latest.FilePath, "application/octet-stream", downloadName, enableRangeProcessing: true);
    }

    /// <summary>What the current tenant's documents &amp; images zip would contain right now.</summary>
    /// <response code="200">File counts and sizes, per upload kind and in total.</response>
    [RequirePermission("identity-administration.view")]
    [HttpGet("mine/files")]
    public async Task<IActionResult> GetMyFiles(CancellationToken cancellationToken)
    {
        var summary = await _filesArchive.GetSummaryAsync(CurrentTenantId(), cancellationToken);
        return Ok(new ApiResponse<TenantFilesSummaryResponse>
        {
            Data = new TenantFilesSummaryResponse
            {
                FileCount = summary.FileCount,
                TotalSizeBytes = summary.TotalSizeBytes,
                Categories = summary.Categories
                    .Select(category => new TenantFileCategoryResponse
                    {
                        Key = category.Key,
                        Label = category.Label,
                        FileCount = category.FileCount,
                        SizeBytes = category.SizeBytes,
                    })
                    .ToList(),
            },
        });
    }

    /// <summary>Zips and streams every document and image the current tenant has uploaded.</summary>
    /// <response code="200">The zip file.</response>
    /// <response code="404">This hospital has no uploaded files yet.</response>
    [RequirePermission("identity-administration.view")]
    [HttpGet("mine/files/download")]
    public async Task<IActionResult> DownloadMyFiles(CancellationToken cancellationToken)
    {
        // Built into a temp file first rather than straight into the response: the response
        // then gets a real Content-Length, and a failure part-way through is still a clean
        // error instead of a 200 with a truncated zip. DeleteOnClose removes the temp file as
        // soon as FileStreamResult disposes the stream after sending it (or below, on failure).
        var zipStream = new FileStream(
            Path.Combine(Path.GetTempPath(), $"hms-files-{Guid.NewGuid():N}.zip"),
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.DeleteOnClose | FileOptions.Asynchronous);

        int fileCount;
        try
        {
            fileCount = await _filesArchive.WriteZipAsync(CurrentTenantId(), zipStream, cancellationToken);
        }
        catch
        {
            await zipStream.DisposeAsync();
            throw;
        }

        if (fileCount == 0)
        {
            await zipStream.DisposeAsync();
            return NotFound(new ApiErrorResponse
            {
                ErrorCode = "BACKUPS.NO_FILES",
                Message = "This hospital has no uploaded documents or images yet.",
                CorrelationId = HttpContext.GetCorrelationId(),
                Timestamp = DateTime.UtcNow,
            });
        }

        zipStream.Position = 0;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, MonthlySeries.HospitalTimeZone));
        return File(zipStream, "application/zip", $"documents-and-images-{today:yyyy-MM-dd}.zip");
    }

    private string CurrentTenantKey() => CurrentTenantId().ToString();

    private Guid CurrentTenantId()
        => _tenantContext.TenantId ?? throw new InvalidOperationException("TenantBackupsController reached without a resolved tenant.");
}
