using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.IPD.Endpoints;

[ApiController]
[RequireFeature("ipd")]
[Route("api/v1/ipd/dashboard")]
public class IPDDashboardController : ControllerBase
{
    private readonly IIPDDashboardService _service;

    public IPDDashboardController(IIPDDashboardService service)
    {
        _service = service;
    }

    [Authorize]
    [RequirePermission("clinical-care.view")]
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var dashboard = await _service.GetDashboardAsync(cancellationToken);
        return Ok(new ApiResponse<IPDDashboardResponse> { Data = dashboard });
    }

    /// <summary>Admissions per month for the last N months (default 6, max 24) — backs the
    /// Executive Dashboard's IP census chart (DASH-01).</summary>
    [Authorize]
    [RequirePermission("clinical-care.view")]
    [HttpGet("monthly-admissions")]
    public async Task<IActionResult> GetMonthlyAdmissions([FromQuery] int months = 6, CancellationToken cancellationToken = default)
    {
        var series = await _service.GetMonthlyAdmissionsAsync(months, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<MonthlyTotal>> { Data = series });
    }
}
