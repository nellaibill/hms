using HMS.Modules.Branding.Application;
using HMS.Modules.Branding.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HMS.Modules.Branding.Endpoints;

/// <summary>
/// The Branding module's HTTP surface — get/update the current theme configuration and
/// upload a hospital logo, per the Theme &amp; Branding settings feature. Every action
/// requires a Hospital JWT (the default FallbackPolicy) so the tenant-aware BrandingDbContext
/// can resolve which hospital's own branding row to read/write — see docs/DecisionLog.md.
/// The pre-login screen no longer calls this endpoint at all; it themes itself with the
/// static frontend defaults instead (no tenant is known yet at that point). "Actor"
/// (updated-by) is read from the caller's JWT via ClaimsPrincipalExtensions.GetUserId.
/// <c>Get</c> is deliberately left at bare <c>[Authorize]</c> — every authenticated user of
/// any role needs it to theme the app after login, not just Settings admins. <c>Update</c>/
/// <c>UploadLogo</c> additionally require <c>identity-administration.edit</c> (reusing the
/// existing "Roles, Users &amp; Settings" category — see ModuleCatalog/ROLE_MODULES rather than
/// adding a new one, matching Masters' own reuse of this module for its Settings-adjacent
/// pages) so a non-admin authenticated user can no longer rewrite tenant-wide branding by
/// calling the API directly — see docs/DecisionLog.md ADR-074.
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

    /// <summary>Gets the current tenant's theme/branding configuration.</summary>
    /// <response code="200">The current branding configuration.</response>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var response = await _brandingService.GetAsync(cancellationToken);
        return Ok(Envelope(response));
    }

    /// <summary>Updates the theme/branding configuration (colors, fonts, hospital identity).</summary>
    /// <response code="200">The branding configuration was updated.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="403">The caller lacks <c>identity-administration.edit</c>.</response>
    [HttpPut]
    [RequirePermission("identity-administration.edit")]
    public async Task<IActionResult> Update([FromBody] UpdateBrandingRequest request, CancellationToken cancellationToken)
    {
        var result = await _brandingService.UpdateAsync(request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Uploads/replaces one logo slot (PNG/JPG/WEBP/SVG, max 500KB, 16-2000px per side for raster formats — content is decoded/sanity-checked, not just trusted by extension). <paramref name="slot"/> is a BrandingLogoSlots value; omitted means the Primary logo, so existing callers are unchanged.</summary>
    /// <response code="200">The logo was uploaded and set.</response>
    /// <response code="400">The file is missing or failed validation.</response>
    /// <response code="403">The caller lacks <c>identity-administration.edit</c>.</response>
    [HttpPost("logo")]
    [Consumes("multipart/form-data")]
    [RequirePermission("identity-administration.edit")]
    [EnableRateLimiting(RateLimitingPolicyNames.Write)]
    public async Task<IActionResult> UploadLogo(IFormFile file, [FromQuery] string? slot, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(BuildFileRequiredError());
        }

        await using var stream = file.OpenReadStream();
        var result = await _brandingService.UploadLogoAsync(slot ?? BrandingLogoSlots.Primary, stream, file.FileName, file.Length, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Removes one logo slot's logo (Primary when <paramref name="slot"/> is omitted); the app then falls back per the logo display settings.</summary>
    /// <response code="200">The logo was removed.</response>
    /// <response code="400">Unknown slot.</response>
    /// <response code="403">The caller lacks <c>identity-administration.edit</c>.</response>
    [HttpDelete("logo")]
    [RequirePermission("identity-administration.edit")]
    [EnableRateLimiting(RateLimitingPolicyNames.Write)]
    public async Task<IActionResult> RemoveLogo([FromQuery] string? slot, CancellationToken cancellationToken)
    {
        var result = await _brandingService.RemoveLogoAsync(slot ?? BrandingLogoSlots.Primary, actorId: User.GetUserId(), cancellationToken);
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
