using HMS.Modules.Backups.Application.Abstractions;
using HMS.Modules.Backups.Contracts;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Backups.Endpoints;

/// <summary>
/// Platform Admin surface for every backup this instance produces — masters plus every active
/// tenant. Gated behind PlatformSuperAdmin specifically, not the weaker Platform policy:
/// downloading a raw multi-tenant database dump is the single most sensitive read in the app.
/// </summary>
[ApiController]
[Route("api/platform/backups")]
[Authorize(Policy = "PlatformSuperAdmin")]
public class PlatformBackupsController : ControllerBase
{
    private const string MastersKey = "masters";

    private readonly IBackupStorage _storage;
    private readonly ITenantDirectory _tenantDirectory;

    public PlatformBackupsController(IBackupStorage storage, ITenantDirectory tenantDirectory)
    {
        _storage = storage;
        _tenantDirectory = tenantDirectory;
    }

    /// <summary>Lists the latest available backup for masters plus every active tenant.</summary>
    /// <response code="200">The list, one entry per masters/active tenant.</response>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var results = new List<BackupSummaryResponse>
        {
            await ToResponseAsync(MastersKey, "Masters", cancellationToken),
        };

        var tenants = await _tenantDirectory.GetAllActiveTenantsAsync(cancellationToken);
        foreach (var tenant in tenants)
        {
            results.Add(await ToResponseAsync(tenant.Id.ToString(), tenant.HospitalCode, cancellationToken));
        }

        return Ok(new ApiResponse<IReadOnlyList<BackupSummaryResponse>> { Data = results });
    }

    /// <summary>Streams the latest backup for the given key ("masters" or a tenant id).</summary>
    /// <response code="200">The backup file.</response>
    /// <response code="404">No backup is available yet for this key.</response>
    [HttpGet("{key}/download")]
    public async Task<IActionResult> Download(string key, CancellationToken cancellationToken)
    {
        var latest = await _storage.GetLatestAsync(key, cancellationToken);
        if (latest is null)
        {
            return NotFound(BuildNotFoundError());
        }

        var downloadName = $"{key}-{latest.Date:yyyy-MM-dd}.dump";
        return PhysicalFile(latest.FilePath, "application/octet-stream", downloadName, enableRangeProcessing: true);
    }

    private async Task<BackupSummaryResponse> ToResponseAsync(string key, string label, CancellationToken cancellationToken)
    {
        var latest = await _storage.GetLatestAsync(key, cancellationToken);
        return new BackupSummaryResponse
        {
            Key = key,
            Label = label,
            LastBackupDate = latest?.Date,
            SizeBytes = latest?.SizeBytes,
            IsAvailable = latest is not null,
        };
    }

    private ApiErrorResponse BuildNotFoundError() => new()
    {
        ErrorCode = "BACKUPS.NOT_FOUND",
        Message = "No backup is available yet for this key.",
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };
}
