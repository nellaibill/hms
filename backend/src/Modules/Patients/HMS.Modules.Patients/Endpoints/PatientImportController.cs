using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Patients.Endpoints;

/// <summary>
/// Bulk patient import — Super Admin only (see PermissionSeedData's
/// "patient-management.import" entry). Every action here either hands back a generated .xlsx
/// or drives PatientImportService; the actual parsing/validation/patient-creation work happens
/// off the request thread — see Infrastructure/PatientImportValidationBackgroundService and
/// PatientImportCommitBackgroundService.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/patients/import")]
public class PatientImportController : ControllerBase
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IPatientImportService _importService;

    public PatientImportController(IPatientImportService importService)
    {
        _importService = importService;
    }

    /// <summary>Downloads the blank Excel template — column layout, dropdowns, and the
    /// Instructions/Required Fields sheet.</summary>
    /// <response code="200">The template file.</response>
    [RequirePermission("patient-management.import")]
    [HttpGet("template")]
    public async Task<IActionResult> GetTemplate(CancellationToken cancellationToken)
    {
        var bytes = await _importService.GetTemplateAsync(cancellationToken);
        return File(bytes, XlsxContentType, "patient_import_template.xlsx");
    }

    /// <summary>Uploads a filled-in template. Queues the validate pass; nothing is written to
    /// patients/addresses by this call.</summary>
    /// <response code="202">The file was accepted and queued for validation.</response>
    /// <response code="400">The file is missing, empty, not an .xlsx, or too large.</response>
    [RequirePermission("patient-management.import")]
    [HttpPost]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(BuildError(PatientErrorCodes.ImportFileInvalid, "A file is required."));
        }

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        var result = await _importService.UploadAsync(file.FileName, stream.ToArray(), User.GetUserId(), cancellationToken);
        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        return AcceptedAtAction(nameof(GetBatch), new { batchId = result.Value!.Id }, Envelope(result.Value));
    }

    /// <summary>Batch status and row counters — poll this while Validating/Committing.</summary>
    /// <response code="200">The batch.</response>
    /// <response code="404">No import batch was found for the given id.</response>
    [RequirePermission("patient-management.import")]
    [HttpGet("{batchId:guid}")]
    public async Task<IActionResult> GetBatch(Guid batchId, CancellationToken cancellationToken)
    {
        var result = await _importService.GetBatchAsync(batchId, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Import History — every past and in-progress batch, newest first.</summary>
    /// <response code="200">A page of import batches.</response>
    [RequirePermission("patient-management.import")]
    [HttpGet]
    public async Task<IActionResult> GetBatches([FromQuery] ImportBatchListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _importService.GetBatchesPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta
        {
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = query.PageSize == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize),
        };

        return Ok(new ApiResponse<IReadOnlyList<ImportBatchResponse>> { Data = items, Meta = meta });
    }

    /// <summary>Paginated row detail for the review screen — filter by Status (e.g. Invalid) to
    /// show only what was skipped.</summary>
    /// <response code="200">A page of rows.</response>
    /// <response code="404">No import batch was found for the given id.</response>
    [RequirePermission("patient-management.import")]
    [HttpGet("{batchId:guid}/rows")]
    public async Task<IActionResult> GetRows(Guid batchId, [FromQuery] ImportRowListQuery query, CancellationToken cancellationToken)
    {
        var result = await _importService.GetRowsPagedAsync(batchId, query, cancellationToken);
        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        var page = result.Value!;
        var meta = new PaginationMeta
        {
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = page.TotalCount,
            TotalPages = query.PageSize == 0 ? 0 : (int)Math.Ceiling(page.TotalCount / (double)query.PageSize),
        };

        return Ok(new ApiResponse<IReadOnlyList<ImportRowResponse>> { Data = page.Items, Meta = meta });
    }

    /// <summary>Downloads every skipped row (Invalid + CommitFailed) with its original data and
    /// the reason(s) it was skipped, in the template's own column layout — fix and re-upload as
    /// a fresh batch.</summary>
    /// <response code="200">The report file.</response>
    /// <response code="404">No import batch was found for the given id.</response>
    [RequirePermission("patient-management.import")]
    [HttpGet("{batchId:guid}/report")]
    public async Task<IActionResult> GetReport(Guid batchId, CancellationToken cancellationToken)
    {
        var result = await _importService.GetReportAsync(batchId, cancellationToken);
        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        return File(result.Value!, XlsxContentType, $"patient_import_{batchId}_errors.xlsx");
    }

    /// <summary>Confirms the import — only valid once the batch is ReadyForReview. Writes
    /// nothing itself; queues the commit pass that actually creates the patients.</summary>
    /// <response code="202">The batch was confirmed and queued for commit.</response>
    /// <response code="404">No import batch was found for the given id.</response>
    /// <response code="409">The batch isn't ReadyForReview (already committed, still
    /// validating, or failed to parse).</response>
    [RequirePermission("patient-management.import")]
    [HttpPost("{batchId:guid}/commit")]
    public async Task<IActionResult> Commit(Guid batchId, CancellationToken cancellationToken)
    {
        var result = await _importService.CommitAsync(batchId, User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Accepted(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<ImportBatchResponse> Envelope(ImportBatchResponse? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            PatientErrorCodes.ImportBatchNotFound => StatusCodes.Status404NotFound,
            PatientErrorCodes.ImportBatchNotReady => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };

        return StatusCode(status, BuildError(errorCode, message));
    }

    private ApiErrorResponse BuildError(string errorCode, string message) => new()
    {
        ErrorCode = errorCode,
        Message = message,
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };
}
