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
/// (already gates the whole Settings area) rather than adding a new one.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/backups")]
public class TenantBackupsController : ControllerBase
{
    private readonly IBackupStorage _storage;
    private readonly ITenantContext _tenantContext;

    public TenantBackupsController(IBackupStorage storage, ITenantContext tenantContext)
    {
        _storage = storage;
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

    private string CurrentTenantKey()
        => (_tenantContext.TenantId ?? throw new InvalidOperationException("TenantBackupsController reached without a resolved tenant.")).ToString();
}
