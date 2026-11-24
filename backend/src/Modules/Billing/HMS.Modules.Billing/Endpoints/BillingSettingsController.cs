using HMS.Modules.Billing.Application;
using HMS.Modules.Billing.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Billing.Endpoints;

/// <summary>
/// Settings → Billing Approvals (ADR-136): whether discounts are allowed, and whether discounts
/// and bill reverts need approval. Reading is open to any signed-in user, because the billing
/// screens need it; saving needs identity-administration.edit, the same grain as the other
/// hospital-wide settings on the Settings page.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/billing/settings")]
public class BillingSettingsController : ControllerBase
{
    private readonly IBillingSettingsService _service;

    public BillingSettingsController(IBillingSettingsService service)
    {
        _service = service;
    }

    /// <summary>The saved settings, or the defaults (discounts allowed, approvals on).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<BillingApprovalSettings>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(new ApiResponse<BillingApprovalSettings> { Data = await _service.GetAsync(cancellationToken) });

    /// <summary>Saves the settings. They apply to the next discount or revert; requests already
    /// waiting for approval stay in the queue.</summary>
    /// <response code="200">The settings were saved.</response>
    /// <response code="400">No settings were sent.</response>
    [RequirePermission("identity-administration.edit")]
    [RequireFeature("settings.billing-approvals")]
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<BillingApprovalSettings>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromBody] BillingApprovalSettings? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new ApiErrorResponse
            {
                ErrorCode = "VALIDATION.FAILED",
                Message = "A request body is required.",
                CorrelationId = HttpContext.GetCorrelationId(),
                Timestamp = DateTime.UtcNow,
            });
        }

        var result = await _service.UpdateAsync(request, User.GetUserId(), cancellationToken);
        return Ok(new ApiResponse<BillingApprovalSettings> { Data = result.Value });
    }
}
