using HMS.Modules.HR.Application;
using HMS.Modules.HR.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.HR.Endpoints;

/// <summary>
/// The Hospital HR Management MVP's dashboard — a single aggregate view for the HR landing
/// page (headcount, today's attendance, pending leave, expiring staff documents). See
/// docs/DecisionLog.md ADR-036.
/// </summary>
[ApiController]
[RequireFeature("hr")]
[Authorize]
[RequirePermission("workforce-admin.view")]
[Route("api/v1/hr/dashboard")]
public class HrDashboardController : ControllerBase
{
    private readonly IHrDashboardService _service;

    public HrDashboardController(IHrDashboardService service)
    {
        _service = service;
    }

    /// <summary>Gets the current HR dashboard snapshot.</summary>
    /// <response code="200">The current dashboard snapshot.</response>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var dashboard = await _service.GetDashboardAsync(cancellationToken);
        return Ok(new ApiResponse<HrDashboardResponse> { Data = dashboard });
    }
}
