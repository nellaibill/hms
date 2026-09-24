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

/// <summary>AI-assisted reading of a patient's stored X-ray images, saved to the patient's record.
/// Output is an unreviewed draft for a clinician, never a diagnosis.</summary>
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

    /// <summary>Analyzes one stored patient image with the configured vision model and saves the
    /// result as an unreviewed draft on the patient's record.</summary>
    /// <response code="200">The saved AI draft analysis.</response>
    /// <response code="404">No such document, or the caller can't see it.</response>
    /// <response code="422">The document isn't a supported patient image, or is too large.</response>
    /// <response code="503">AI analysis isn't configured, or the AI service failed.</response>
    [RequirePermission("diagnostics.create")]
    [EnableRateLimiting(RateLimitingPolicyNames.Write)]
    [HttpPost("ai-analysis/documents/{documentId:guid}")]
    public async Task<IActionResult> AnalyzeDocument(Guid documentId, CancellationToken cancellationToken)
    {
        var result = await _service.AnalyzeDocumentAsync(documentId, GetActor(), cancellationToken);
        return result.IsSuccess ? Ok(new ApiResponse<XrayAiAnalysisResponse> { Data = result.Value }) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Lists the saved AI analyses of a patient's images (newest first), limited to the
    /// images the caller can see.</summary>
    /// <response code="200">The saved analyses.</response>
    [RequirePermission("diagnostics.view")]
    [HttpGet("ai-analysis/patients/{patientId:guid}")]
    public async Task<IActionResult> GetPatientAnalyses(Guid patientId, CancellationToken cancellationToken)
    {
        var result = await _service.GetPatientAnalysesAsync(patientId, GetActor(), cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<IReadOnlyList<XrayAiAnalysisResponse>> { Data = result.Value })
            : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Marks a saved AI draft as reviewed by the calling doctor/radiologist (idempotent).</summary>
    /// <response code="200">The analysis, now marked reviewed.</response>
    /// <response code="403">The caller isn't a doctor or radiologist.</response>
    /// <response code="404">No such analysis, or the caller can't see its image.</response>
    [RequirePermission("diagnostics.edit")]
    [HttpPost("ai-analysis/{analysisId:guid}/review")]
    public async Task<IActionResult> MarkReviewed(Guid analysisId, CancellationToken cancellationToken)
    {
        var result = await _service.MarkReviewedAsync(analysisId, GetActor(), cancellationToken);
        return result.IsSuccess ? Ok(new ApiResponse<XrayAiAnalysisResponse> { Data = result.Value }) : MapFailure(result.ErrorCode!, result.Error!);
    }

    private DocumentActor GetActor()
    {
        var userId = Guid.TryParse(User.FindFirst("UserId")?.Value, out var parsed) ? parsed : (Guid?)null;
        return new DocumentActor(userId, User.FindFirst("LoginType")?.Value);
    }

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            RadiologyAiErrorCodes.DocumentNotFound or RadiologyAiErrorCodes.AnalysisNotFound => StatusCodes.Status404NotFound,
            RadiologyAiErrorCodes.DocumentNotAvailable => StatusCodes.Status409Conflict,
            RadiologyAiErrorCodes.ReviewForbidden => StatusCodes.Status403Forbidden,
            RadiologyAiErrorCodes.UnsupportedImage or RadiologyAiErrorCodes.ImageTooLarge or RadiologyAiErrorCodes.NotPatientDocument => StatusCodes.Status422UnprocessableEntity,
            RadiologyAiErrorCodes.NotConfigured or RadiologyAiErrorCodes.RequestFailed => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status400BadRequest,
        };

        return StatusCode(status, new ApiErrorResponse
        {
            ErrorCode = errorCode,
            Message = message,
            CorrelationId = HttpContext.GetCorrelationId(),
            Timestamp = DateTime.UtcNow,
        });
    }
}
