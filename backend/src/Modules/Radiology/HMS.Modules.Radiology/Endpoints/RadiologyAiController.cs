using HMS.Modules.Documents.Application;
using HMS.Modules.Radiology.Application;
using HMS.Modules.Radiology.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HMS.Modules.Radiology.Endpoints;

/// <summary>AI-assisted reading of stored X-ray images. Output is an unreviewed draft for a
/// clinician, never a diagnosis.</summary>
[ApiController]
[Authorize]
[Route("api/v1/radiology")]
public class RadiologyAiController : ControllerBase
{
    private readonly IRadiologyAiService _service;

    public RadiologyAiController(IRadiologyAiService service)
    {
        _service = service;
    }

    /// <summary>Analyzes one stored image document with the configured vision model.</summary>
    /// <response code="200">The AI draft analysis.</response>
    /// <response code="404">No such document, or the caller can't see it.</response>
    /// <response code="422">The document isn't a supported image, or is too large.</response>
    /// <response code="503">AI analysis isn't configured, or the AI service failed.</response>
    [RequirePermission("diagnostics.view")]
    [EnableRateLimiting(RateLimitingPolicyNames.Write)]
    [HttpPost("ai-analysis/documents/{documentId:guid}")]
    public async Task<IActionResult> AnalyzeDocument(Guid documentId, CancellationToken cancellationToken)
    {
        var userId = Guid.TryParse(User.FindFirst("UserId")?.Value, out var parsed) ? parsed : (Guid?)null;
        var actor = new DocumentActor(userId, User.FindFirst("LoginType")?.Value);

        var result = await _service.AnalyzeDocumentAsync(documentId, actor, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(new ApiResponse<XrayAiAnalysisResponse> { Data = result.Value });
        }

        var status = result.ErrorCode switch
        {
            RadiologyAiErrorCodes.DocumentNotFound => StatusCodes.Status404NotFound,
            RadiologyAiErrorCodes.DocumentNotAvailable => StatusCodes.Status409Conflict,
            RadiologyAiErrorCodes.UnsupportedImage or RadiologyAiErrorCodes.ImageTooLarge => StatusCodes.Status422UnprocessableEntity,
            RadiologyAiErrorCodes.NotConfigured or RadiologyAiErrorCodes.RequestFailed => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status400BadRequest,
        };

        return StatusCode(status, new ApiErrorResponse
        {
            ErrorCode = result.ErrorCode!,
            Message = result.Error!,
            CorrelationId = HttpContext.GetCorrelationId(),
            Timestamp = DateTime.UtcNow,
        });
    }
}
