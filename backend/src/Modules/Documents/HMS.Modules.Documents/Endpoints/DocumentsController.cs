using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Documents.Application;
using HMS.Modules.Documents.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Documents.Endpoints;

/// <summary>
/// The Documents module's HTTP surface — a generic, cross-module document repository keyed
/// by owner type + owner id (US-1 through US-8; see
/// docs/modules/Documents/DocumentManagement.md). Unlike every other module's controller in
/// this codebase, this one requires authentication and derives a real
/// <see cref="DocumentActor"/> from the caller's JWT claims rather than passing
/// <c>actorId: null</c> — Documents aggregates the most sensitive artifact of every other
/// module in one place, so RBAC could not be deferred the way it has been elsewhere (US-2).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/documents")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IValidator<UploadDocumentRequest> _uploadValidator;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentService documentService,
        IValidator<UploadDocumentRequest> uploadValidator,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _uploadValidator = uploadValidator;
        _logger = logger;
    }

    /// <summary>Uploads a file and attaches it to a specific owner record (US-1).</summary>
    /// <response code="201">The document was uploaded and queued for a virus scan.</response>
    /// <response code="400">The request or file failed validation.</response>
    /// <response code="403">The caller may not upload documents for this owner type.</response>
    /// <response code="404">No record of the given owner type/id was found.</response>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] UploadDocumentRequest request, CancellationToken cancellationToken)
    {
        var validation = await _uploadValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(BuildFileRequiredError());
        }

        await using var stream = file.OpenReadStream();
        var actor = GetActor();
        var result = await _documentService.UploadAsync(request, stream, file.FileName, file.ContentType, file.Length, actor, cancellationToken);

        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        _logger.LogInformation("POST /api/v1/documents succeeded for document {DocumentId} (actor {UserId})", result.Value!.Id, actor.UserId);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, Envelope(result.Value));
    }

    /// <summary>Lists documents with paging, filtering, and search (US-3).</summary>
    /// <response code="200">A page of documents visible to the caller.</response>
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] DocumentListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _documentService.GetPagedAsync(query, GetActor(), cancellationToken);

        var meta = new PaginationMeta
        {
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            TotalPages = paged.TotalPages,
        };

        return Ok(new ApiResponse<IReadOnlyList<DocumentResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Server-side KPI aggregate — total, uploaded today, archived, storage used (US-7).</summary>
    /// <response code="200">The current repository-wide summary.</response>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _documentService.GetSummaryAsync(GetActor(), cancellationToken);
        return Ok(new ApiResponse<DocumentSummaryResponse> { Data = result.Value });
    }

    /// <summary>Gets a single document's metadata by id.</summary>
    /// <response code="200">The document was found and is visible to the caller.</response>
    /// <response code="404">No document was found, or it exists but the caller can't see it.</response>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _documentService.GetByIdAsync(id, GetActor(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>
    /// Streams a document's bytes after the same access check as every other read (US-4).
    /// This is the <em>only</em> way to retrieve a document's content — it is never served via
    /// static file middleware (see docs/modules/Documents/DocumentManagement.md's security
    /// review).
    /// </summary>
    /// <response code="200">The file content.</response>
    /// <response code="404">No document was found, or it exists but the caller can't see it.</response>
    /// <response code="409">The document exists but its content isn't available yet (still
    /// being scanned, or quarantined).</response>
    [HttpGet("{id:guid}/content")]
    public async Task<IActionResult> GetContent(Guid id, CancellationToken cancellationToken)
    {
        var actor = GetActor();
        var result = await _documentService.GetContentAsync(id, actor, cancellationToken);
        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        _logger.LogInformation("GET /api/v1/documents/{DocumentId}/content served to {UserId}", id, actor.UserId);

        var content = result.Value!;
        return File(content.Content, content.ContentType, content.FileName, enableRangeProcessing: true);
    }

    /// <summary>Archives a document — hidden from active searches, not deleted (US-5, idempotent).</summary>
    /// <response code="200">The document is archived.</response>
    /// <response code="403">The caller may not archive documents for this owner type.</response>
    /// <response code="404">No document was found, or it exists but the caller can't see it.</response>
    [HttpPatch("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _documentService.ArchiveAsync(id, GetActor(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Soft-deletes a document — the row and its file on disk are retained for audit
    /// purposes, only <c>IsDeleted</c> is set (US-6).</summary>
    /// <response code="204">The document was soft-deleted.</response>
    /// <response code="403">The caller may not delete documents for this owner type.</response>
    /// <response code="404">No document was found, or it exists but the caller can't see it.</response>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _documentService.DeleteAsync(id, GetActor(), cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Reads the authenticated caller's identity off the validated JWT (US-2) — see
    /// DocumentActor's remarks for why "LoginType," not the freeform "RoleName," is the claim
    /// used to drive access decisions.</summary>
    private DocumentActor GetActor()
    {
        var userId = Guid.TryParse(User.FindFirst("UserId")?.Value, out var parsed) ? parsed : (Guid?)null;
        var loginType = User.FindFirst("LoginType")?.Value;
        return new DocumentActor(userId, loginType);
    }

    private static ApiResponse<DocumentResponse> Envelope(DocumentResponse? data) => new() { Data = data };

    private ApiErrorResponse BuildFileRequiredError() => new()
    {
        ErrorCode = "VALIDATION.FAILED",
        Message = "A file is required.",
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            DocumentErrorCodes.NotFound => StatusCodes.Status404NotFound,
            DocumentErrorCodes.OwnerNotFound => StatusCodes.Status404NotFound,
            DocumentErrorCodes.Forbidden => StatusCodes.Status403Forbidden,
            DocumentErrorCodes.NotAvailable => StatusCodes.Status409Conflict,
            DocumentErrorCodes.InvalidFile => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest,
        };

        var error = new ApiErrorResponse
        {
            ErrorCode = errorCode,
            Message = message,
            CorrelationId = HttpContext.GetCorrelationId(),
            Timestamp = DateTime.UtcNow,
        };

        return StatusCode(status, error);
    }

    private ApiErrorResponse BuildValidationError(ValidationResult validation) => new()
    {
        ErrorCode = "VALIDATION.FAILED",
        Message = "One or more validation errors occurred.",
        ValidationErrors = validation.Errors
            .Select(e => new ValidationErrorItem { Field = e.PropertyName, Message = e.ErrorMessage })
            .ToList(),
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };
}
