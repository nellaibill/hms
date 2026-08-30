using HMS.Modules.Branding.Application;
using HMS.Modules.Branding.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Branding.Endpoints;

/// <summary>
/// The Branding module's HTTP surface — get/update the current theme configuration and
/// upload a hospital logo, per the Theme &amp; Branding settings feature. GET has no
/// authentication so the pre-login screen can theme itself too; PUT/POST match that same
/// (currently app-wide) posture — see docs/DecisionLog.md. "Actor" (updated-by) is read
/// from the caller's JWT via ClaimsPrincipalExtensions.GetUserId when one is present.
/// </summary>
[ApiController]
[Route("api/v1/branding")]
public class BrandingController : ControllerBase
{
    private readonly IBrandingService _brandingService;

    public BrandingController(IBrandingService brandingService)
    {
        _brandingService = brandingService;
    }

    /// <summary>Gets the current theme/branding configuration. Public — no auth — so the pre-login screen can theme itself.</summary>
    /// <response code="200">The current branding configuration.</response>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var response = await _brandingService.GetAsync(cancellationToken);
        return Ok(Envelope(response));
    }

    /// <summary>Updates the theme/branding configuration (colors, fonts, hospital identity).</summary>
    /// <response code="200">The branding configuration was updated.</response>
    /// <response code="400">The request failed validation.</response>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateBrandingRequest request, CancellationToken cancellationToken)
    {
        var result = await _brandingService.UpdateAsync(request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Uploads/replaces the hospital logo (PNG/JPG/WEBP/SVG, max 500KB, 16-2000px per side for raster formats — content is decoded/sanity-checked, not just trusted by extension).</summary>
    /// <response code="200">The logo was uploaded and set.</response>
    /// <response code="400">The file is missing or failed validation.</response>
    [HttpPost("logo")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadLogo(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(BuildFileRequiredError());
        }

        await using var stream = file.OpenReadStream();
        var result = await _brandingService.UploadLogoAsync(stream, file.FileName, file.Length, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<BrandingResponse> Envelope(BrandingResponse? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var error = new ApiErrorResponse
        {
            ErrorCode = errorCode,
            Message = message,
            CorrelationId = HttpContext.GetCorrelationId(),
            Timestamp = DateTime.UtcNow,
        };

        return BadRequest(error);
    }

    private ApiErrorResponse BuildFileRequiredError() => new()
    {
        ErrorCode = "VALIDATION.FAILED",
        Message = "A file is required.",
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };
}
